using Content.Shared._White.Amputatable.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Gibbable.Systems;
using Content.Shared._White.Random;
using Content.Shared._White.Skeleton.Systems;
using Content.Shared._White.Threshold;
using Content.Shared._White.Wounds.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._White.Amputatable.Systems;

public sealed class AmputatableSystem : EntitySystem
{
    [Dependency] private readonly IPredictedRandom _random = default!;

    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly WoundableSystem _woundable = default!;

    private const float LaunchImpulse = 8;
    private const float LaunchImpulseVariance = 3;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmputatableProviderComponent, BodyProviderGotInsertedIntoParentEvent>(OnGotInsertedIntoParent);
        SubscribeLocalEvent<AmputatableProviderComponent, BodyProviderGotRemovedFromParentEvent>(OnGotRemovedFromParent);
        SubscribeLocalEvent<AmputatableProviderComponent, BodyRelayedEvent<BeingGibbedEvent>>(OnBeingGibbed);
        SubscribeLocalEvent<AmputatableProviderComponent, SkeletonSeverityChangedEvent>(OnSkeletonSeverityChanged);
        SubscribeLocalEvent<AmputatableProviderComponent, WoundableDamageChangedEvent>(OnWoundableDamageChanged);
    }

    #region Event Handling

    private void OnGotInsertedIntoParent(Entity<AmputatableProviderComponent> ent, ref BodyProviderGotInsertedIntoParentEvent args)
    {
        ent.Comp.Parent = args.Parent;
        DirtyField(ent, ent.Comp, nameof(AmputatableProviderComponent.Parent));
    }

    private void OnGotRemovedFromParent(Entity<AmputatableProviderComponent> ent, ref BodyProviderGotRemovedFromParentEvent args)
    {
        ent.Comp.Parent = null;
        DirtyField(ent, ent.Comp, nameof(AmputatableProviderComponent.Parent));
    }

    private static void OnBeingGibbed(Entity<AmputatableProviderComponent> ent, ref BodyRelayedEvent<BeingGibbedEvent> args)
    {
        args.Args.Giblets.Add(ent);
    }

    private void OnSkeletonSeverityChanged(Entity<AmputatableProviderComponent> ent, ref SkeletonSeverityChangedEvent args)
    {
        if (!ent.Comp.SkeletonThresholds.TryGetValue(args.Severity, out var skeletonMultiplier))
            return;

        ent.Comp.SkeletonMultiplier = skeletonMultiplier;
        DirtyField(ent, ent.Comp, nameof(AmputatableProviderComponent.SkeletonMultiplier));
    }

    private void OnWoundableDamageChanged(Entity<AmputatableProviderComponent> ent, ref WoundableDamageChangedEvent args)
    {
        var damage = FixedPoint2.Zero;
        foreach (var (type, value) in args.Damage)
        {
            if (!ent.Comp.SupportedDamage.TryGetValue(type, out var modify))
                continue;

            damage = value * modify;
        }

        if (damage == FixedPoint2.Zero)
            return;

        ent.Comp.Damage += damage;
        DirtyField(ent, ent.Comp, nameof(AmputatableProviderComponent.Damage));

        ent.Comp.Chance = ent.Comp.Thresholds.HighestMatch(ent.Comp.Damage) ?? 0f;
        DirtyField(ent, ent.Comp, nameof(AmputatableProviderComponent.Chance));

        var random = _random.GetRandom(ent);
        if (!random.Prob(ent.Comp.CurrentChance))
            return;

        _body.TryDetachProvider(ent.Owner);

        var impulse = LaunchImpulse + random.NextFloat(LaunchImpulseVariance);
        var scatterVec = random.NextAngle().ToVec() * impulse;

        _physics.ApplyLinearImpulse(ent, scatterVec);

        if (ent.Comp.Wound is not {} wound || ent.Comp.Parent is not {} parent)
            return;

        _woundable.CreateWound(parent, wound, ent.Comp.Damage, args.Origin);
    }

    #endregion
}
