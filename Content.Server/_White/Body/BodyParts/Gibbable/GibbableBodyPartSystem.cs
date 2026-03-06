using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Body.Wounds.Systems;
using Content.Shared._White.Gibbing;
using Content.Shared._White.Threshold;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;
using Robust.Shared.Random;

namespace Content.Server._White.Body.BodyParts.Gibbable;

public sealed class GibbableBodyPartSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly SharedWoundSystem _wound = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GibbableBodyPartComponent, BodyPartRelayedEvent<RejuvenateEvent>>(OnRejuvenate);
        SubscribeLocalEvent<GibbableBodyPartComponent, BoneStatusChangedEvent>(OnBoneStatusChange);
        SubscribeLocalEvent<GibbableBodyPartComponent, WoundDamageChangedEvent>(OnWoundDamageChanged);
    }

    private void OnRejuvenate(Entity<GibbableBodyPartComponent> ent, ref BodyPartRelayedEvent<RejuvenateEvent> args)
    {
        ent.Comp.TotalDamage = FixedPoint2.Zero;
        ent.Comp.CurrentChanceThreshold = ent.Comp.ChanceThresholds.HighestMatch(ent.Comp.TotalDamage) ?? 0f;
    }

    private void OnBoneStatusChange(Entity<GibbableBodyPartComponent> ent, ref BoneStatusChangedEvent args)
    {
        if (!ent.Comp.BoneMultiplierThresholds.TryGetValue(args.BoneState, out var boneMultiplier))
            return;

        ent.Comp.CurrentBoneMultiplierThreshold = boneMultiplier;
    }

    private void OnWoundDamageChanged(Entity<GibbableBodyPartComponent> ent, ref WoundDamageChangedEvent args)
    {
        if (args.Wound.Comp.IsScar || !ent.Comp.SupportedDamageType.TryGetValue(args.Wound.Comp.DamageType, out var modify))
            return;

        ent.Comp.TotalDamage += (args.Wound.Comp.Damage - args.OldDamage) * modify;

        if (args.OldDamage >= args.Wound.Comp.Damage)
            return;

        ent.Comp.CurrentChanceThreshold = ent.Comp.ChanceThresholds.HighestMatch(ent.Comp.TotalDamage) ?? 0f;

        if (!_random.Prob(ent.Comp.CurrentChance))
            return;

        if (TryComp<BodyPartComponent>(ent, out var bodyPartComponent)
            && bodyPartComponent.Parent.HasValue
            && _wound.TryCreateWound(bodyPartComponent.Parent.Value, ent.Comp.Wound, out var wound))
            _wound.ChangeWoundDamage(wound.Value.AsNullable(), ent.Comp.TotalDamage);

        _gibbing.Gib(ent, user: args.Origin);
    }
}
