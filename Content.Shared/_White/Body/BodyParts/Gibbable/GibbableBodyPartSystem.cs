using Content.Shared._White.Body.Components;
using Content.Shared._White.Gibbing;
using Content.Shared._White.Medical.Wounds.Systems;
using Content.Shared._White.Random;
using Content.Shared._White.Threshold;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Throwing;

namespace Content.Shared._White.Body.BodyParts.Gibbable;

public sealed class GibbableBodyPartSystem : EntitySystem
{
    [Dependency] private readonly PredictedRandomManager _random = default!;

    [Dependency] private readonly SharedGibbingSystem _gibbing = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

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

    private void OnDamageChanged(Entity<GibbableBodyPartComponent> gibbableBodyPart, ref DamageChangedEvent args)
    {
        if (args.DamageDelta != null)
        {
            var damageDelta = FixedPoint2.Zero;
            foreach (var (damageType, damageValue) in args.DamageDelta.DamageDict)
            {
                if (damageValue <= 0 || !gibbableBodyPart.Comp.SupportedDamageType.Contains(damageType))
                    continue;

                damageDelta += damageValue;
            }

            if (damageDelta == FixedPoint2.Zero)
                return;
        }

        var totalDamage = FixedPoint2.Zero;
        foreach (var (damageType, damageValue) in args.Damageable.Damage.DamageDict)
        {
            if (damageValue <= 0 || !gibbableBodyPart.Comp.SupportedDamageType.Contains(damageType))
                continue;

            totalDamage += damageValue;
        }

        if (totalDamage  == FixedPoint2.Zero)
            return;

        gibbableBodyPart.Comp.CurrentChanceThreshold = gibbableBodyPart.Comp.ChanceThresholds.HighestMatch(totalDamage) ?? 0f;

        if (!_random.Prob(gibbableBodyPart, gibbableBodyPart.Comp.CurrentChance))
            return;

        var outerEntity = gibbableBodyPart.Owner;
        if (TryComp<BodyPartComponent>(gibbableBodyPart,  out var bodyPartComponent) && bodyPartComponent.Body.HasValue)
            outerEntity = bodyPartComponent.Body.Value;

        var droppedEntities = new HashSet<EntityUid>();
        _gibbing.TryGibEntity(outerEntity, gibbableBodyPart.Owner, GibType.Gib, GibType.Drop, ref droppedEntities);

        foreach (var entity in droppedEntities)
        {
            var random = _random.GetRandom(entity);
            _throwing.TryThrow(
                entity,
                _random.NextAngle(random).ToWorldVec() * _random.NextFloat(random, 0.8f, 2f),
                _random.NextFloat(random, 0.5f, 1f),
                pushbackRatio: 0.3f);
        }
    }
}
