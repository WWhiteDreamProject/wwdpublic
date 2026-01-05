using Content.Shared._White.Body.BodyParts.Amputatable;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Body.Wounds.Systems;
using Content.Shared._White.Gibbing;
using Content.Shared._White.Threshold;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;
using Content.Shared.Throwing;
using Robust.Shared.Random;

namespace Content.Server._White.Body.BodyParts.Gibbable;

public sealed class GibbableBodyPartSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly SharedGibbingSystem _gibbing = default!;
    [Dependency] private readonly SharedWoundSystem _wound = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GibbableBodyPartComponent, BodyPartRelayedEvent<RejuvenateEvent>>(OnRejuvenate);
        SubscribeLocalEvent<GibbableBodyPartComponent, BoneStatusChangedEvent>(OnBoneStatusChange);
        SubscribeLocalEvent<GibbableBodyPartComponent, WoundDamageChangedEvent>(OnWoundDamageChanged, after: new [] {typeof(AmputatableBodyPartSystem)});
    }

    private void OnRejuvenate(Entity<GibbableBodyPartComponent> gibbableBodyPart, ref BodyPartRelayedEvent<RejuvenateEvent> args)
    {
        gibbableBodyPart.Comp.TotalDamage = FixedPoint2.Zero;
        gibbableBodyPart.Comp.CurrentChanceThreshold = gibbableBodyPart.Comp.ChanceThresholds.HighestMatch(gibbableBodyPart.Comp.TotalDamage) ?? 0f;
    }

    private void OnBoneStatusChange(Entity<GibbableBodyPartComponent> gibbableBodyPart, ref BoneStatusChangedEvent args)
    {
        if (!gibbableBodyPart.Comp.BoneMultiplierThresholds.TryGetValue(args.BoneState, out var boneMultiplier))
            return;

        gibbableBodyPart.Comp.CurrentBoneMultiplierThreshold = boneMultiplier;
    }

    private void OnWoundDamageChanged(Entity<GibbableBodyPartComponent> gibbableBodyPart, ref WoundDamageChangedEvent args)
    {
        if (args.Handled
            || args.Wound.Comp.IsScar
            || !gibbableBodyPart.Comp.SupportedDamageType.TryGetValue(args.Wound.Comp.DamageType, out var modify))
            return;

        gibbableBodyPart.Comp.TotalDamage += (args.Wound.Comp.DamageAmount - args.OldDamage) * modify;

        if (args.OldDamage >= args.Wound.Comp.DamageAmount)
            return;

        gibbableBodyPart.Comp.CurrentChanceThreshold = gibbableBodyPart.Comp.ChanceThresholds.HighestMatch(gibbableBodyPart.Comp.TotalDamage) ?? 0f;

        if (!_random.Prob(gibbableBodyPart.Comp.CurrentChance))
            return;

        var outerEntity = gibbableBodyPart.Owner;
        if (TryComp<BodyPartComponent>(gibbableBodyPart, out var bodyPartComponent))
        {
            if (bodyPartComponent.Body.HasValue)
                outerEntity = bodyPartComponent.Body.Value;

            if (bodyPartComponent.Parent.HasValue
                && _wound.TryCreateWound(bodyPartComponent.Parent.Value, gibbableBodyPart.Comp.Wound, out var wound))
                _wound.ChangeWoundDamage(wound.Value.AsNullable(), gibbableBodyPart.Comp.TotalDamage);
        }

        var droppedEntities = new HashSet<EntityUid>();
        _gibbing.TryGibEntity(outerEntity, gibbableBodyPart.Owner, GibType.Gib, GibType.Drop, ref droppedEntities, false);

        foreach (var entity in droppedEntities)
        {
            _throwing.TryThrow(
                entity,
                _random.NextAngle().ToWorldVec() * _random.NextFloat(0.8f, 2f),
                _random.NextFloat(0.5f, 1f),
                pushbackRatio: 0.3f);
        }

        args.Handled = true;
    }
}
