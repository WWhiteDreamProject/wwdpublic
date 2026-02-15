using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Body.Wounds.Systems;
using Content.Shared._White.Random;
using Content.Shared._White.Threshold;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;
using Content.Shared.Throwing;
using Robust.Shared.Timing;

namespace Content.Shared._White.Body.BodyParts.Amputatable;

public sealed class AmputatableBodyPartSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPredictedRandom _random = default!;

    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedWoundSystem _wound = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AmputatableBodyPartComponent, BodyPartRelayedEvent<RejuvenateEvent>>(OnRejuvenate);
        SubscribeLocalEvent<AmputatableBodyPartComponent, BoneStatusChangedEvent>(OnBoneStatusChange);
        SubscribeLocalEvent<AmputatableBodyPartComponent, WoundDamageChangedEvent>(OnWoundDamageChanged);
    }

    private void OnRejuvenate(Entity<AmputatableBodyPartComponent> amputatableBodyPart, ref BodyPartRelayedEvent<RejuvenateEvent> args)
    {
        amputatableBodyPart.Comp.TotalDamage = FixedPoint2.Zero;
        amputatableBodyPart.Comp.CurrentChanceThreshold = amputatableBodyPart.Comp.ChanceThresholds.HighestMatch(amputatableBodyPart.Comp.TotalDamage) ?? 0f;
    }

    private void OnBoneStatusChange(Entity<AmputatableBodyPartComponent> amputatableBodyPart, ref BoneStatusChangedEvent args)
    {
        if (!amputatableBodyPart.Comp.BoneMultiplierThresholds.TryGetValue(args.BoneState, out var boneMultiplier))
            return;

        amputatableBodyPart.Comp.CurrentBoneMultiplierThreshold = boneMultiplier;
    }

    private void OnWoundDamageChanged(Entity<AmputatableBodyPartComponent> amputatableBodyPart, ref WoundDamageChangedEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted
            || args.Handled
            || args.Wound.Comp.IsScar
            || !amputatableBodyPart.Comp.SupportedDamageType.TryGetValue(args.Wound.Comp.DamageType, out var modify))
            return;

        amputatableBodyPart.Comp.TotalDamage += (args.Wound.Comp.Damage - args.OldDamage) * modify;

        if (args.OldDamage >= args.Wound.Comp.Damage)
            return;

        amputatableBodyPart.Comp.CurrentChanceThreshold = amputatableBodyPart.Comp.ChanceThresholds.HighestMatch(amputatableBodyPart.Comp.TotalDamage) ?? 0f;

        var random = _random.GetRandom(amputatableBodyPart);
        if (!random.Prob(amputatableBodyPart.Comp.CurrentChance))
            return;

        if (TryComp<BodyPartComponent>(amputatableBodyPart, out var bodyPartComponent)
            && bodyPartComponent.Parent.HasValue
            && _wound.TryCreateWound(bodyPartComponent.Parent.Value, amputatableBodyPart.Comp.Wound, out var wound))
            _wound.ChangeWoundDamage(wound.Value.AsNullable(), amputatableBodyPart.Comp.TotalDamage);

        _body.TryDetachBodyPart(amputatableBodyPart);
        _throwing.TryThrow(
            amputatableBodyPart,
            random.NextAngle().ToWorldVec() * random.NextFloat(0.8f, 2f),
            random.NextFloat(0.5f, 1f),
            pushbackRatio: 0.3f);

        args.Handled = true;
    }
}
