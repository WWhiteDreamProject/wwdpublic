using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Medical.Wounds.Systems;
using Content.Shared._White.Random;
using Content.Shared._White.Threshold;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._White.Body.BodyParts.Amputatable;

public sealed class AmputatableBodyPartSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PredictedRandomManager _random = default!;

    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedWoundSystem _wound = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AmputatableBodyPartComponent, BoneStatusChangedEvent>(OnBoneStatusChange);
        SubscribeLocalEvent<AmputatableBodyPartComponent, WoundDamageChangedEvent>(OnWoundDamageChanged);
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
            || args.Wound.Comp.IsScar
            || !amputatableBodyPart.Comp.SupportedDamageType.Contains(args.Wound.Comp.DamageType)
            || args.OldDamage <= args.Wound.Comp.DamageAmount)
            return;

        amputatableBodyPart.Comp.CurrentChanceThreshold = amputatableBodyPart.Comp.ChanceThresholds.HighestMatch(args.Wound.Comp.DamageAmount) ?? 0f;

        var random = _random.GetRandom(amputatableBodyPart);
        if (!_random.Prob(random, amputatableBodyPart.Comp.CurrentChance))
            return;

        if (TryComp<BodyPartComponent>(amputatableBodyPart, out var bodyPart)
            && bodyPart.Parent.HasValue
            && _wound.TryCreateWound(bodyPart.Parent.Value, amputatableBodyPart.Comp.Wound, out var wound))
            _wound.ChangeWoundDamage(wound.Value.AsNullable(), args.Wound.Comp.DamageAmount);

        _body.TryDetachBodyPart(amputatableBodyPart);
        _throwing.TryThrow(
            amputatableBodyPart,
            _random.NextAngle(random).ToWorldVec() * _random.NextFloat(random, 0.8f, 2f),
            _random.NextFloat(random, 0.5f, 1f),
            pushbackRatio: 0.3f);
    }
}
