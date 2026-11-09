using Content.Shared._White.Random;
using Content.Shared._White.Wounds.Systems;
using Content.Shared.Destructible;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._White.Gibbable.Systems;

public sealed partial class GibbableSystem : EntitySystem
{
    [Dependency] private readonly IPredictedRandom _random = default!;

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly WoundableSystem _woundable = default!;

    private const float GibletLaunchImpulse = 8;
    private const float GibletLaunchImpulseVariance = 3;

    private static readonly SoundSpecifier? GibSound = new SoundCollectionSpecifier("gib", AudioParams.Default.WithVariation(0.025f));

    public override void Initialize()
    {
        base.Initialize();

        InitializeProvider();
    }

    #region Public API

    /// <summary>
    /// Gibs an entity.
    /// </summary>
    /// <param name="ent">The entity to gib.</param>
    /// <param name="dropGiblets">Whether to drop giblets.</param>
    /// <returns>The set of giblets for this entity, if any.</returns>
    public HashSet<EntityUid> Gib(EntityUid ent, bool dropGiblets = true)
    {
        var giblets = new HashSet<EntityUid>();
        if (!_destructible.DestroyEntity(ent))
            return giblets;

        _audio.PlayPvs(GibSound, ent);

        var beingGibbedEv = new BeingGibbedEvent(giblets);
        RaiseLocalEvent(ent, ref beingGibbedEv);

        if (dropGiblets)
        {
            foreach (var giblet in giblets)
            {
                _transform.DropNextTo(giblet, ent);

                var random = _random.GetRandom(giblet);
                var impulse = GibletLaunchImpulse + random.NextFloat(GibletLaunchImpulseVariance);
                var scatterVec = random.NextAngle().ToVec() * impulse;

                _physics.ApplyLinearImpulse(giblet, scatterVec);
            }
        }

        var beforeDeletionEv = new GibbedBeforeDeletionEvent(giblets);
        RaiseLocalEvent(ent, ref beforeDeletionEv);

        return giblets;
    }

    #endregion
}

/// <summary>
/// Event raised on an entity when it is being gibbed.
/// </summary>
/// <param name="Giblets">If a component wants to provide giblets to scatter, add them to this hashset.</param>
[ByRefEvent]
public readonly record struct BeingGibbedEvent(HashSet<EntityUid> Giblets);

/// <summary>
/// Event raised on an entity when it is about to be deleted after being gibbed.
/// </summary>
/// <param name="Giblets">The set of giblets this entity produced.</param>
[ByRefEvent]
public readonly record struct GibbedBeforeDeletionEvent(HashSet<EntityUid> Giblets);
