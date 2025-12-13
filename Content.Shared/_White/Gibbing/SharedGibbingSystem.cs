using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared._White.Gibbing;

public abstract class SharedGibbingSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float GibletLaunchImpulse = 8;
    private const float GibletLaunchImpulseVariance = 3;

    public bool TryGibBody(
        Entity<BodyComponent?, GibbableComponent?> body,
        out HashSet<EntityUid> gibs,
        bool gibOrgans = false,
        Vector2? splatDirection = null,
        float splatModifier = 1,
        Angle splatCone = default,
        SoundSpecifier? gibSoundOverride = null
    )
    {
        gibs = GibBody(body, gibOrgans, splatDirection, splatModifier, splatCone, gibSoundOverride);
        if (gibs.Count != 0)
            return true;

        return false;
    }

    public virtual HashSet<EntityUid> GibBody(
        Entity<BodyComponent?, GibbableComponent?> body,
        bool gibOrgans = false,
        Vector2? splatDirection = null,
        float splatModifier = 1,
        Angle splatCone = default,
        SoundSpecifier? gibSoundOverride = null
        )
    {
        var gibs = new HashSet<EntityUid>();

        if (!Resolve(body, ref body.Comp1, logMissing: false))
            return gibs;

        if (Resolve(body, ref body.Comp2, logMissing: false))
            gibSoundOverride ??= body.Comp2.GibSound;

        var parts = _body.GetBodyParts((body, body.Comp1)).ToArray();
        gibs.EnsureCapacity(parts.Length);

        foreach (var part in parts)
        {
            TryGibEntity(
                body,
                part.Owner,
                GibType.Gib,
                GibType.Skip,
                ref gibs,
                playAudio: false,
                launchGibs:true,
                launchDirection:splatDirection,
                launchImpulse: GibletLaunchImpulse * splatModifier,
                launchImpulseVariance:GibletLaunchImpulseVariance,
                launchCone: splatCone);

            if (!gibOrgans)
                continue;

            foreach (var (_, organContainer) in part.Comp.Organs)
            {
                if (organContainer.OrganUid is null)
                    continue;

                TryGibEntity(
                    body,
                    organContainer.OrganUid.Value,
                    GibType.Drop,
                    GibType.Skip,
                    ref gibs,
                    playAudio: false,
                    launchImpulse: GibletLaunchImpulse * splatModifier,
                    launchImpulseVariance:GibletLaunchImpulseVariance,
                    launchCone: splatCone);
            }
        }

        if (HasComp<InventoryComponent>(body))
        {
            foreach (var item in _inventory.GetHandOrInventoryEntities(body.Owner))
            {
                _transform.AttachToGridOrMap(item);
                gibs.Add(item);
            }
        }

        _audio.PlayPredicted(gibSoundOverride, Transform(body).Coordinates, null);
        return gibs;
    }

    #region PrivateAPI

    private bool TryGibEntity(
        EntityUid outerEntity,
        Entity<GibbableComponent?> gibbable,
        GibType gibType,
        GibType gibOption,
        ref HashSet<EntityUid> droppedEntities,
        bool launchGibs = true,
        Vector2? launchDirection = null,
        float launchImpulse = 0f,
        float launchImpulseVariance = 0f,
        Angle launchCone = default,
        float randomSpreadMod = 1.0f,
        bool playAudio = true,
        List<string>? allowedContainers = null,
        List<string>? excludedContainers = null,
        bool logMissingGibable = false
        )
    {
        if (!Resolve(gibbable, ref gibbable.Comp, logMissing: false))
        {
            DropEntity(
                gibbable,
                Transform(outerEntity),
                randomSpreadMod,
                ref droppedEntities,
                launchGibs,
                launchDirection,
                launchImpulse,
                launchImpulseVariance,
                launchCone);

            if (logMissingGibable)
            {
                Log.Warning(
                    $"{ToPrettyString(gibbable)} does not have a GibbableComponent! " +
                            $"This is not required but may cause issues contained items to not be dropped.");
            }

            return false;
        }

        if (gibType == GibType.Skip && gibOption == GibType.Skip)
            return true;

        if (launchGibs)
            randomSpreadMod = 0;

        var parentXform = Transform(outerEntity);
        HashSet<BaseContainer> validContainers = new();

        var gibContentsAttempt =
            new AttemptEntityContentsGibEvent(gibbable, gibOption, allowedContainers, excludedContainers);
        RaiseLocalEvent(gibbable, ref gibContentsAttempt);

        foreach (var container in _container.GetAllContainers(gibbable))
        {
            var valid = true;
            if (allowedContainers != null)
                valid = allowedContainers.Contains(container.ID);
            if (excludedContainers != null)
                valid = valid && !excludedContainers.Contains(container.ID);
            if (valid)
                validContainers.Add(container);
        }

        switch (gibOption)
        {
            case GibType.Skip:
                break;
            case GibType.Drop:
            {
                foreach (var container in validContainers)
                {
                    var entities = container.ContainedEntities.ToList();
                    foreach(var ent in entities)
                    {
                        DropEntity(
                            new Entity<GibbableComponent?>(ent, null),
                            parentXform,
                            randomSpreadMod,
                            ref droppedEntities,
                            launchGibs,
                            launchDirection,
                            launchImpulse,
                            launchImpulseVariance,
                            launchCone);
                    }
                }

                break;
            }
            case GibType.Gib:
            {
                foreach (var container in validContainers)
                {
                    var entities = container.ContainedEntities.ToList();
                    foreach(var ent in entities)
                    {
                        GibEntity(
                            new Entity<GibbableComponent?>(ent, null),
                            parentXform,
                            randomSpreadMod,
                            ref droppedEntities,
                            launchGibs,
                            launchDirection,
                            launchImpulse,
                            launchImpulseVariance,
                            launchCone);
                    }
                }

                break;
            }
        }

        switch (gibType)
        {
            case GibType.Skip:
                break;
            case GibType.Drop:
            {
                DropEntity(
                    gibbable,
                    parentXform,
                    randomSpreadMod,
                    ref droppedEntities,
                    launchGibs,
                    launchDirection,
                    launchImpulse,
                    launchImpulseVariance,
                    launchCone);
                break;
            }
            case GibType.Gib:
            {
                GibEntity(
                    gibbable,
                    parentXform,
                    randomSpreadMod,
                    ref droppedEntities,
                    launchGibs,
                    launchDirection,
                    launchImpulse,
                    launchImpulseVariance,
                    launchCone);
                QueueDel(gibbable);
                break;
            }
        }

        if (playAudio)
            _audio.PlayPredicted(gibbable.Comp.GibSound, parentXform.Coordinates, null);

        return true;
    }

    private void DropEntity(
        Entity<GibbableComponent?> gibbable,
        TransformComponent parentXform,
        float randomSpreadMod,
        ref HashSet<EntityUid> droppedEntities,
        bool flingEntity,
        Vector2? scatterDirection,
        float scatterImpulse,
        float scatterImpulseVariance,
        Angle scatterCone
        )
    {
        var gibCount = 0;
        if (Resolve(gibbable, ref gibbable.Comp, logMissing: false))
            gibCount = gibbable.Comp.GibCount;

        var gibAttemptEvent = new AttemptEntityGibEvent(gibbable, gibCount, GibType.Drop);
        RaiseLocalEvent(gibbable, ref gibAttemptEvent);
        switch (gibAttemptEvent.GibType)
        {
            case GibType.Skip:
                return;
            case GibType.Gib:
                GibEntity(
                    gibbable,
                    parentXform,
                    randomSpreadMod,
                    ref droppedEntities,
                    flingEntity,
                    scatterDirection,
                    scatterImpulse,
                    scatterImpulseVariance,
                    scatterCone,
                    deleteTarget: false);
                return;
        }

        _transform.AttachToGridOrMap(gibbable);
        _transform.SetCoordinates(gibbable, parentXform.Coordinates);
        _transform.SetWorldRotation(gibbable, _random.NextAngle());

        droppedEntities.Add(gibbable);

        if (flingEntity)
            FlingDroppedEntity(gibbable, scatterDirection, scatterImpulse, scatterImpulseVariance, scatterCone);

        var gibbedEvent = new EntityGibbedEvent(gibbable, new List<EntityUid> {gibbable, });
        RaiseLocalEvent(gibbable, ref gibbedEvent);
    }

    private void GibEntity(
        Entity<GibbableComponent?> gibbable,
        TransformComponent parentXform,
        float randomSpreadMod,
        ref HashSet<EntityUid> droppedEntities,
        bool flingEntity,
        Vector2? scatterDirection,
        float scatterImpulse,
        float scatterImpulseVariance,
        Angle scatterCone,
        bool deleteTarget = true
    )
    {
        var localGibs = new List<EntityUid>();
        var gibCount = 0;
        var gibProtoCount = 0;

        if (Resolve(gibbable, ref gibbable.Comp, logMissing: false))
        {
            gibCount = gibbable.Comp.GibCount;
            gibProtoCount = gibbable.Comp.GibPrototypes.Count;
        }

        var gibAttemptEvent = new AttemptEntityGibEvent(gibbable, gibCount, GibType.Drop);
        RaiseLocalEvent(gibbable, ref gibAttemptEvent);

        switch (gibAttemptEvent.GibType)
        {
            case GibType.Skip:
                return;
            case GibType.Drop:
                DropEntity(
                    gibbable,
                    parentXform,
                    randomSpreadMod,
                    ref droppedEntities,
                    flingEntity,
                    scatterDirection,
                    scatterImpulse,
                    scatterImpulseVariance,
                    scatterCone);

                localGibs.Add(gibbable);
                return;
        }

        if (gibbable.Comp != null && gibProtoCount > 0)
        {
            if (flingEntity)
            {
                for (var i = 0; i < gibAttemptEvent.GibletCount; i++)
                {
                    if (!TryCreateRandomGiblet(
                        gibbable.Comp,
                        parentXform.Coordinates,
                        false,
                        out var giblet,
                        randomSpreadMod))
                        continue;

                    FlingDroppedEntity(
                        giblet.Value,
                        scatterDirection,
                        scatterImpulse,
                        scatterImpulseVariance,
                        scatterCone);

                    droppedEntities.Add(giblet.Value);
                }
            }
            else
            {
                for (var i = 0; i < gibAttemptEvent.GibletCount; i++)
                {
                    if (TryCreateRandomGiblet(
                        gibbable.Comp,
                        parentXform.Coordinates,
                        false,
                        out var giblet,
                        randomSpreadMod))
                        droppedEntities.Add(giblet.Value);
                }
            }
        }

        _transform.AttachToGridOrMap(gibbable, Transform(gibbable));

        if (flingEntity)
            FlingDroppedEntity(gibbable, scatterDirection, scatterImpulse, scatterImpulseVariance, scatterCone);

        var gibbedEvent = new EntityGibbedEvent(gibbable, localGibs);
        RaiseLocalEvent(gibbable, ref gibbedEvent);

        if (deleteTarget)
            QueueDel(gibbable);
    }

    private void FlingDroppedEntity(
        EntityUid target,
        Vector2? direction,
        float impulse,
        float impulseVariance,
        Angle scatterConeAngle
        )
    {
        var scatterAngle = direction?.ToAngle() ?? _random.NextAngle();
        var scatterVector = _random.NextAngle(scatterAngle - scatterConeAngle / 2, scatterAngle + scatterConeAngle / 2)
            .ToVec() * (impulse + _random.NextFloat(impulseVariance));
        _physics.ApplyLinearImpulse(target, scatterVector);
    }

    private bool TryCreateRandomGiblet(
        GibbableComponent gibbable,
        EntityCoordinates coords,
        bool playSound,
        [NotNullWhen(true)] out EntityUid? gibletEntity,
        float? randomSpreadModifier = null
        )
    {
        gibletEntity = null;
        if (gibbable.GibPrototypes.Count == 0)
            return false;

        gibletEntity = Spawn(
            gibbable.GibPrototypes[_random.Next(0, gibbable.GibPrototypes.Count)],
            randomSpreadModifier == null
                ? coords
                : coords.Offset(_random.NextVector2(gibbable.GibScatterRange * randomSpreadModifier.Value)));

        if (playSound)
            _audio.PlayPredicted(gibbable.GibSound, coords, null);

        _transform.SetWorldRotation(gibletEntity.Value, _random.NextAngle());
        return true;
    }

    #endregion
}

/// <summary>
/// Called just before we actually gib the target entity
/// </summary>
/// <param name="Target">The entity being gibed</param>
/// <param name="GibType">What type of gibbing is occuring</param>
/// <param name="AllowedContainers">Containers we are allow to gib</param>
/// <param name="ExcludedContainers">Containers we are allow not allowed to gib</param>
[ByRefEvent] public record struct AttemptEntityContentsGibEvent(
    EntityUid Target,
    GibType GibType,
    List<string>? AllowedContainers,
    List<string>? ExcludedContainers
);

/// <summary>
/// Called just before we actually gib the target entity
/// </summary>
/// <param name="Target">The entity being gibed</param>
/// <param name="GibletCount">how many giblets to spawn</param>
/// <param name="GibType">What type of gibbing is occuring</param>
[ByRefEvent] public record struct AttemptEntityGibEvent(EntityUid Target, int GibletCount, GibType GibType);

/// <summary>
/// Called immediately after we gib the target entity
/// </summary>
/// <param name="Target">The entity being gibbed</param>
/// <param name="DroppedEntities">Any entities that are spilled out (if any)</param>
[ByRefEvent] public record struct EntityGibbedEvent(EntityUid Target, List<EntityUid> DroppedEntities);


