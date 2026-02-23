using Content.Shared.Destructible;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared._White.Gibbing;

public sealed class GibbingSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float GibletLaunchImpulse = 8;
    private const float GibletLaunchImpulseVariance = 3;

    private static readonly SoundSpecifier? GibSound = new SoundCollectionSpecifier("gib", AudioParams.Default.WithVariation(0.025f));

    /// <summary>
    /// Gibs an entity.
    /// </summary>
    /// <param name="ent">The entity to gib.</param>
    /// <param name="dropGiblets">Whether to drop giblets.</param>
    /// <param name="user">The user gibbing the entity, if any.</param>
    /// <returns>The set of giblets for this entity, if any.</returns>
    public HashSet<EntityUid> Gib(EntityUid ent, bool dropGiblets = true, EntityUid? user = null)
    {
        _destructible.DestroyEntity(ent);

        _audio.PlayPvs(GibSound, ent);

        var giblets = new HashSet<EntityUid>();

        var beingGibbedEv = new BeingGibbedEvent(giblets);
        RaiseLocalEvent(ent, ref beingGibbedEv);

        if (dropGiblets)
        {
            foreach (var giblet in giblets)
            {
                _transform.DropNextTo(giblet, ent);
                FlingDroppedEntity(giblet);
            }
        }

        var beforeDeletionEv = new GibbedBeforeDeletionEvent(giblets);
        RaiseLocalEvent(ent, ref beforeDeletionEv);

        return giblets;
    }

    private void FlingDroppedEntity(EntityUid target)
    {
        var impulse = GibletLaunchImpulse + _random.NextFloat(GibletLaunchImpulseVariance);
        var scatterVec = _random.NextAngle().ToVec() * impulse;
        _physics.ApplyLinearImpulse(target, scatterVec);
    }
}

/// <summary>
/// Raised on an entity when it is being gibbed.
/// </summary>
/// <param name="Giblets">If a component wants to provide giblets to scatter, add them to this hashset.</param>
[ByRefEvent]
public readonly record struct BeingGibbedEvent(HashSet<EntityUid> Giblets);

/// <summary>
/// Raised on an entity when it is about to be deleted after being gibbed.
/// </summary>
/// <param name="Giblets">The set of giblets this entity produced.</param>
[ByRefEvent]
public readonly record struct GibbedBeforeDeletionEvent(HashSet<EntityUid> Giblets);
