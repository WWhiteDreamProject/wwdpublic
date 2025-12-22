using Content.Shared._White.Body.Systems;
using Content.Shared._White.Medical.Wounds.Systems;
using Content.Shared._White.Threshold;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._White.Body.BodyParts.Amputatable;

public sealed class AmputatableBodyPartSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;

    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AmputatableBodyPartComponent, BoneStatusChangedOnBodyPartEvent>(OnBoneStatusChange);
        SubscribeLocalEvent<AmputatableBodyPartComponent, DamageChangedEvent>(OnDamageChanged, after: new [] {typeof(SharedWoundSystem), });
    }

    private void OnBoneStatusChange(
        Entity<AmputatableBodyPartComponent> amputatableBodyPart,
        ref BoneStatusChangedOnBodyPartEvent args
    )
    {
        if (!amputatableBodyPart.Comp.BoneMultiplierThresholds.TryGetValue(args.BoneState, out var boneMultiplier))
            return;

        amputatableBodyPart.Comp.CurrentBoneMultiplierThreshold = boneMultiplier;
    }

    private void OnDamageChanged(Entity<AmputatableBodyPartComponent> amputatableBodyPart, ref DamageChangedEvent args)
    {
        if (args.DamageDelta != null)
        {
            var damageDelta = FixedPoint2.Zero;
            foreach (var (damageType, damageValue) in args.DamageDelta.DamageDict)
            {
                if (damageValue <= 0 || !amputatableBodyPart.Comp.SupportedDamageType.Contains(damageType))
                    continue;

                damageDelta += damageValue;
            }

            if (damageDelta == FixedPoint2.Zero)
                return;
        }

        var totalDamage = FixedPoint2.Zero;
        foreach (var (damageType, damageValue) in args.Damageable.Damage.DamageDict)
        {
            if (damageValue <= 0 || !amputatableBodyPart.Comp.SupportedDamageType.Contains(damageType))
                continue;

            totalDamage += damageValue;
        }

        if (totalDamage  == FixedPoint2.Zero)
            return;

        amputatableBodyPart.Comp.CurrentChanceThreshold = amputatableBodyPart.Comp.ChanceThresholds.HighestMatch(totalDamage) ?? 0f;

        if (!_random.Prob(amputatableBodyPart.Comp.CurrentChance) || _net.IsClient)
            return;

        _body.TryDetachBodyPart(amputatableBodyPart);
        _throwing.TryThrow(
            amputatableBodyPart,
            _random.NextAngle().ToWorldVec() * _random.NextFloat(0.8f, 2f),
            _random.NextFloat(0.5f, 1f),
            pushbackRatio: 0.3f);
    }
}
