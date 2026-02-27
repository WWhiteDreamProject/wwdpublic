using Content.Shared._White.Body.Systems;
using Content.Shared._White.Body.Wounds.Systems;
using Content.Shared._White.Random;
using Content.Shared._White.Threshold;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._White.Body.Providers.Amputatable;

public sealed class AmputatableBodyProviderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPredictedRandom _random = default!;

    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedWoundSystem _wound = default!;

    private const float LaunchImpulse = 8;
    private const float LaunchImpulseVariance = 3;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmputatableBodyProviderComponent, BodyProviderGotInsertedIntoParentEvent>(OnGotInsertedIntoParent);
        SubscribeLocalEvent<AmputatableBodyProviderComponent, BodyProviderGotRemovedFromParentEvent>(OnGotRemovedFromParent);
        SubscribeLocalEvent<AmputatableBodyProviderComponent, BodyRelayedEvent<RejuvenateEvent>>(OnRejuvenate);
        SubscribeLocalEvent<AmputatableBodyProviderComponent, BoneStatusChangedEvent>(OnBoneStatusChange);
        SubscribeLocalEvent<AmputatableBodyProviderComponent, WoundDamageChangedEvent>(OnWoundDamageChanged);
    }

    private void OnGotInsertedIntoParent(Entity<AmputatableBodyProviderComponent> ent, ref BodyProviderGotInsertedIntoParentEvent args)
    {
        ent.Comp.Parent = args.Parent;
        DirtyField(ent, ent.Comp, nameof(AmputatableBodyProviderComponent.Parent));
    }

    private void OnGotRemovedFromParent(Entity<AmputatableBodyProviderComponent> ent, ref BodyProviderGotRemovedFromParentEvent args)
    {
        ent.Comp.Parent = null;
        DirtyField(ent, ent.Comp, nameof(AmputatableBodyProviderComponent.Parent));
    }

    private void OnRejuvenate(Entity<AmputatableBodyProviderComponent> ent, ref BodyRelayedEvent<RejuvenateEvent> args)
    {
        ent.Comp.TotalDamage = FixedPoint2.Zero;
        ent.Comp.CurrentChanceThreshold = ent.Comp.ChanceThresholds.HighestMatch(ent.Comp.TotalDamage) ?? 0f;
        DirtyFields(ent, ent.Comp, null, nameof(AmputatableBodyProviderComponent.TotalDamage), nameof(AmputatableBodyProviderComponent.CurrentChanceThreshold));
    }

    private void OnBoneStatusChange(Entity<AmputatableBodyProviderComponent> ent, ref BoneStatusChangedEvent args)
    {
        if (!ent.Comp.BoneMultiplierThresholds.TryGetValue(args.BoneState, out var boneMultiplier))
            return;

        ent.Comp.CurrentBoneMultiplierThreshold = boneMultiplier;
        DirtyField(ent, ent.Comp, nameof(AmputatableBodyProviderComponent.CurrentBoneMultiplierThreshold));
    }

    private void OnWoundDamageChanged(Entity<AmputatableBodyProviderComponent> ent, ref WoundDamageChangedEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted
            || args.Wound.Comp.IsScar
            || !ent.Comp.SupportedDamageType.TryGetValue(args.Wound.Comp.DamageType, out var modify))
            return;

        ent.Comp.TotalDamage += (args.Wound.Comp.Damage - args.OldDamage) * modify;
        DirtyField(ent, ent.Comp, nameof(AmputatableBodyProviderComponent.TotalDamage));

        if (args.OldDamage >= args.Wound.Comp.Damage)
            return;

        ent.Comp.CurrentChanceThreshold = ent.Comp.ChanceThresholds.HighestMatch(ent.Comp.TotalDamage) ?? 0f;
        DirtyField(ent, ent.Comp, nameof(AmputatableBodyProviderComponent.CurrentChanceThreshold));

        var random = _random.GetRandom(ent);
        if (!random.Prob(ent.Comp.CurrentChance))
            return;

        if (ent.Comp.Parent is {} parent && _wound.TryCreateWound(parent, ent.Comp.Wound, out var wound))
            _wound.ChangeWoundDamage(wound.Value.AsNullable(), ent.Comp.TotalDamage);

        if (!_body.TryDetachProvider(ent.Owner))
            return;

        var impulse = LaunchImpulse + random.NextFloat(LaunchImpulseVariance);
        var scatterVec = random.NextAngle().ToVec() * impulse;

        _physics.ApplyLinearImpulse(ent, scatterVec);
    }
}
