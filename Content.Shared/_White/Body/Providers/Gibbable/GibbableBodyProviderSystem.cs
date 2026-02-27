using Content.Shared._White.Body.Systems;
using Content.Shared._White.Body.Wounds.Systems;
using Content.Shared._White.Gibbing;
using Content.Shared._White.Random;
using Content.Shared._White.Threshold;
using Content.Shared._White.Wounds.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;
using Robust.Shared.Timing;

namespace Content.Shared._White.Body.Providers.Gibbable;

public sealed class GibbableBodyProviderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPredictedRandom _random = default!;

    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly SharedWoundSystem _wound = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GibbableBodyProviderComponent, BodyProviderGotInsertedIntoParentEvent>(OnGotInsertedIntoParent);
        SubscribeLocalEvent<GibbableBodyProviderComponent, BodyProviderGotRemovedFromParentEvent>(OnGotRemovedFromParent);
        SubscribeLocalEvent<GibbableBodyProviderComponent, BodyRelayedEvent<BeingGibbedEvent>>(OnBeingGibbed);
        SubscribeLocalEvent<GibbableBodyProviderComponent, BodyRelayedEvent<RejuvenateEvent>>(OnRejuvenate);
        SubscribeLocalEvent<GibbableBodyProviderComponent, BoneStatusChangedEvent>(OnBoneStatusChange);
        SubscribeLocalEvent<GibbableBodyProviderComponent, WoundDamageChangedEvent>(OnWoundDamageChanged);
    }

    private void OnGotInsertedIntoParent(Entity<GibbableBodyProviderComponent> ent, ref BodyProviderGotInsertedIntoParentEvent args)
    {
        ent.Comp.Parent = args.Parent;
        DirtyField(ent, ent.Comp, nameof(GibbableBodyProviderComponent.Parent));
    }

    private void OnGotRemovedFromParent(Entity<GibbableBodyProviderComponent> ent, ref BodyProviderGotRemovedFromParentEvent args)
    {
        ent.Comp.Parent = null;
        DirtyField(ent, ent.Comp, nameof(GibbableBodyProviderComponent.Parent));
    }

    private static void OnBeingGibbed(Entity<GibbableBodyProviderComponent> ent, ref BodyRelayedEvent<BeingGibbedEvent> args)
    {
        args.Args.Giblets.Add(ent);
    }

    private void OnRejuvenate(Entity<GibbableBodyProviderComponent> ent, ref BodyRelayedEvent<RejuvenateEvent> args)
    {
        ent.Comp.TotalDamage = FixedPoint2.Zero;
        ent.Comp.CurrentChanceThreshold = ent.Comp.ChanceThresholds.HighestMatch(ent.Comp.TotalDamage) ?? 0f;
        DirtyFields(ent, ent.Comp, null, nameof(GibbableBodyProviderComponent.TotalDamage), nameof(GibbableBodyProviderComponent.CurrentChanceThreshold));
    }

    private void OnBoneStatusChange(Entity<GibbableBodyProviderComponent> ent, ref BoneStatusChangedEvent args)
    {
        if (!ent.Comp.BoneMultiplierThresholds.TryGetValue(args.BoneState, out var boneMultiplier))
            return;

        ent.Comp.CurrentBoneMultiplierThreshold = boneMultiplier;
        DirtyField(ent, ent.Comp, nameof(GibbableBodyProviderComponent.CurrentBoneMultiplierThreshold));
    }

    private void OnWoundDamageChanged(Entity<GibbableBodyProviderComponent> ent, ref WoundDamageChangedEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted
            || args.Wound.Comp.IsScar
            || !ent.Comp.SupportedDamageType.TryGetValue(args.Wound.Comp.DamageType, out var modify))
            return;

        ent.Comp.TotalDamage += (args.Wound.Comp.Damage - args.OldDamage) * modify;
        DirtyField(ent, ent.Comp, nameof(GibbableBodyProviderComponent.TotalDamage));

        if (args.OldDamage >= args.Wound.Comp.Damage)
            return;

        ent.Comp.CurrentChanceThreshold = ent.Comp.ChanceThresholds.HighestMatch(ent.Comp.TotalDamage) ?? 0f;
        DirtyField(ent, ent.Comp, nameof(GibbableBodyProviderComponent.CurrentChanceThreshold));

        if (!_random.Prob(ent, ent.Comp.CurrentChance))
            return;

        if (ent.Comp.Parent is {} parent && _wound.TryCreateWound(parent, ent.Comp.Wound, out var wound))
            _wound.ChangeWoundDamage(wound.Value.AsNullable(), ent.Comp.TotalDamage);

        _gibbing.Gib(ent, user: args.Origin);
    }
}
