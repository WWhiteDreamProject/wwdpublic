using Content.Shared._White.Gibbing;
using Content.Shared._White.Medical.Wounds.Systems;
using Content.Shared._White.Random;
using Content.Shared._White.Threshold;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Body.BodyParts.Gibbable;

public sealed class GibbableBodyPartSystem : EntitySystem
{
    [Dependency] private readonly PredictedRandomManager _random = default!;

    [Dependency] private readonly SharedGibbingSystem _gibbing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GibbableBodyPartComponent, BoneStatusChangedEvent>(OnBoneStatusChange);
        SubscribeLocalEvent<GibbableBodyPartComponent, DamageChangedEvent>(OnDamageChanged, after: new [] {typeof(SharedWoundSystem), });
    }

    private void OnBoneStatusChange(Entity<GibbableBodyPartComponent> gibbableBodyPart, ref BoneStatusChangedEvent args)
    {
        if (!gibbableBodyPart.Comp.BoneMultiplierThresholds.TryGetValue(args.BoneState, out var boneMultiplier))
            return;

        gibbableBodyPart.Comp.CurrentBoneMultiplierThreshold = boneMultiplier;
    }

    private void OnDamageChanged(Entity<GibbableBodyPartComponent> amputatableBodyPart, ref DamageChangedEvent args)
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

        var random = _random.GetRandom(amputatableBodyPart);
        if (!_random.Prob(random, amputatableBodyPart.Comp.CurrentChance))
            return;

        _gibbing.G(amputatableBodyPart);
    }
}
