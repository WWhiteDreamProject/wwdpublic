using Content.Shared._White.Body.Systems;
using Content.Shared._White.Gibbable.Components;
using Content.Shared._White.Random;
using Content.Shared._White.Skeleton.Systems;
using Content.Shared._White.Threshold;
using Content.Shared._White.Wounds.Systems;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Gibbable.Systems;

public sealed partial class GibbableSystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<GibbableProviderComponent, BodyProviderGotInsertedIntoParentEvent>(OnGotInsertedIntoParent);
        SubscribeLocalEvent<GibbableProviderComponent, BodyProviderGotRemovedFromParentEvent>(OnGotRemovedFromParent);
        SubscribeLocalEvent<GibbableProviderComponent, BodyRelayedEvent<BeingGibbedEvent>>(OnBeingGibbed);
        SubscribeLocalEvent<GibbableProviderComponent, SkeletonSeverityChangedEvent>(OnSkeletonSeverityChanged);
        SubscribeLocalEvent<GibbableProviderComponent, WoundableDamageChangedEvent>(OnWoundableDamageChanged);
    }

    #region Event Handling

    private void OnGotInsertedIntoParent(Entity<GibbableProviderComponent> ent, ref BodyProviderGotInsertedIntoParentEvent args)
    {
        ent.Comp.Parent = args.Parent;
        DirtyField(ent, ent.Comp, nameof(GibbableProviderComponent.Parent));
    }

    private void OnGotRemovedFromParent(Entity<GibbableProviderComponent> ent, ref BodyProviderGotRemovedFromParentEvent args)
    {
        ent.Comp.Parent = null;
        DirtyField(ent, ent.Comp, nameof(GibbableProviderComponent.Parent));
    }

    private static void OnBeingGibbed(Entity<GibbableProviderComponent> ent, ref BodyRelayedEvent<BeingGibbedEvent> args)
    {
        args.Args.Giblets.Add(ent);
    }

    private void OnSkeletonSeverityChanged(Entity<GibbableProviderComponent> ent, ref SkeletonSeverityChangedEvent args)
    {
        if (!ent.Comp.SkeletonThresholds.TryGetValue(args.Severity, out var skeletonMultiplier))
            return;

        ent.Comp.SkeletonMultiplier = skeletonMultiplier;
        DirtyField(ent, ent.Comp, nameof(GibbableProviderComponent.SkeletonMultiplier));
    }

    private void OnWoundableDamageChanged(Entity<GibbableProviderComponent> ent, ref WoundableDamageChangedEvent args)
    {
        var damage = FixedPoint2.Zero;
        foreach (var (type, value) in args.Damage.DamageDict)
        {
            if (!ent.Comp.SupportedDamage.TryGetValue(type, out var modify))
                continue;

            damage = value * modify;
        }

        if (damage == FixedPoint2.Zero)
            return;

        ent.Comp.Damage += damage;
        DirtyField(ent, ent.Comp, nameof(GibbableProviderComponent.Damage));

        ent.Comp.Chance = ent.Comp.Thresholds.HighestMatch(ent.Comp.Damage) ?? 0f;
        DirtyField(ent, ent.Comp, nameof(GibbableProviderComponent.Chance));

        if (!_random.Prob(ent, ent.Comp.CurrentChance))
            return;

        Gib(ent);

        if (ent.Comp.Wound is not {} wound || ent.Comp.Parent is not {} parent)
            return;

        _woundable.CreateWound(parent, wound, ent.Comp.Damage, args.Origin);
    }

    #endregion
}
