using Content.Shared._White.Body.Components;
using Content.Shared._White.Gibbing;
using Content.Shared._White.Medical.Wounds.Components;
using Content.Shared._White.Threshold;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._White.Medical.Wounds.Systems;

public abstract partial class SharedWoundSystem
{
    private void InitializeBodyPart()
    {
        SubscribeLocalEvent<WoundableBodyPartComponent, AttemptEntityContentsGibEvent>(OnAttemptEntityContentsGib);
        SubscribeLocalEvent<WoundableBodyPartComponent, ComponentInit>(OnBodyPartInit);
        SubscribeLocalEvent<WoundableBodyPartComponent, DamageChangedEvent>(OnBodyPartDamageChanged);
        SubscribeLocalEvent<WoundableBodyPartComponent, RejuvenateEvent>(OnBodyPartRejuvenate);
    }

    #region Event Handling

    private void OnAttemptEntityContentsGib(Entity<WoundableBodyPartComponent> woundableBodyPart, ref AttemptEntityContentsGibEvent args)
    {
        foreach (var wound in GetWounds(woundableBodyPart.AsNullable(), scar:true))
            PredictedQueueDel(wound.Owner);
    }

    private void OnBodyPartInit(Entity<WoundableBodyPartComponent> woundableBodyPart, ref ComponentInit args) =>
        woundableBodyPart.Comp.Container = _container.EnsureContainer<Container>(woundableBodyPart.Owner, WoundsContainerId);

    private void OnBodyPartDamageChanged(Entity<WoundableBodyPartComponent> woundableBodyPart, ref DamageChangedEvent args)
    {
        if (args.DamageDelta?.AnyPositive() != true)
            return;

        foreach (var bone in _body.GetBones<WoundableBoneComponent>(woundableBodyPart))
            ApplyBoneDamage(bone.AsNullable(), args.DamageDelta);
    }

    private void OnBodyPartRejuvenate(Entity<WoundableBodyPartComponent> woundableBodyPart, ref RejuvenateEvent args)
    {
        if (!TryComp<DamageableComponent>(woundableBodyPart, out var damageable))
            return;

        foreach (var wound in GetWounds(woundableBodyPart.AsNullable(), scar:true))
            PredictedQueueDel(wound.Owner);

        Damageable.SetAllDamage(woundableBodyPart, damageable, FixedPoint2.Zero);
    }

    #endregion

    #region Public API

    public List<Entity<WoundableBodyPartComponent>> GetWoundableBodyParts(Entity<WoundableComponent?> woundable, string? damageType = null, BodyPartType bodyPartType = BodyPartType.All)
    {
        if (!Resolve(woundable, ref woundable.Comp))
            return new List<Entity<WoundableBodyPartComponent>>();

        var woundableBodyParts = new List<Entity<WoundableBodyPartComponent>>();
        foreach (var bodyPart in _body.GetBodyParts<WoundableBodyPartComponent>(woundable.Owner, bodyPartType))
        {
            if (!HasWounds((bodyPart, bodyPart.Comp2), damageType))
                continue;

            woundableBodyParts.Add((bodyPart, bodyPart.Comp2));
        }

        return woundableBodyParts;
    }

    public List<Entity<BodyPartComponent, WoundableBodyPartComponent>> GetWoundableBodyParts(Entity<WoundableComponent?> woundable, DamageSpecifier damage, BodyPartType bodyPartType = BodyPartType.All)
    {
        if (!Resolve(woundable, ref woundable.Comp))
            return new List<Entity<BodyPartComponent, WoundableBodyPartComponent>>();

        var bodyParts = new List<Entity<BodyPartComponent, WoundableBodyPartComponent>>();
        foreach (var bodyPart in _body.GetBodyParts<WoundableBodyPartComponent>(woundable.Owner, bodyPartType))
        {
            if (!HasWounds((bodyPart, bodyPart.Comp2), damage))
                continue;

            bodyParts.Add(bodyPart);
        }

        return bodyParts;
    }

    #endregion

    #region Private API

    private DamageSpecifier ApplyBodyPartDamage(
        Entity<BodyPartComponent?, WoundableBodyPartComponent?, DamageableComponent?> bodyPart,
        DamageSpecifier damage,
        Entity<WoundableComponent?>? woundable = null,
        bool ignoreResistance = false
        )
    {
        var bodyPartDamage = new DamageSpecifier();

        if (!Resolve(bodyPart, ref bodyPart.Comp1, ref bodyPart.Comp2, ref bodyPart.Comp3))
            return bodyPartDamage;

        woundable ??= bodyPart.Comp1.Body;

        foreach (var (damageType, damageValue) in damage.DamageDict)
        {
            if (damageValue == 0
                || !bodyPart.Comp3.Damage.DamageDict.ContainsKey(damageType)
                || !_prototype.TryIndex<DamageTypePrototype>(damageType, out var damageTypePrototype))
                continue;

            var newDamageValue = damageValue;
            if (!ignoreResistance)
            {
                if (_prototype.TryIndex(bodyPart.Comp3.DamageModifierSetId, out var modifierSet))
                {
                    var floatDamageValue = damageValue.Float();
                    if (modifierSet.FlatReduction.TryGetValue(damageType, out var reduction))
                        floatDamageValue = Math.Max(0f, floatDamageValue - reduction);

                    if (modifierSet.Coefficients.TryGetValue(damageType, out var coefficient))
                        floatDamageValue *= coefficient;

                    newDamageValue = FixedPoint2.New(floatDamageValue);
                }

                var modifyDamage = new DamageSpecifier();
                modifyDamage.DamageDict.Add(damageType, newDamageValue);

                if (woundable.HasValue)
                {
                    var damageModify = new DamageModifyEvent(modifyDamage, woundable, bodyPart.Comp1.Type);
                    RaiseLocalEvent(woundable.Value, damageModify);

                    newDamageValue = damageModify.Damage[damageType];
                }
            }

            var wound = GetWounds((bodyPart, bodyPart.Comp2), damageType).FirstOrNull();
            if (!wound.HasValue
                && (damageTypePrototype.Wound is not { } woundPrototype
                    || !TryCreateWound(
                        (bodyPart, bodyPart.Comp1, bodyPart.Comp2),
                        woundPrototype,
                        out wound,
                        woundable)))
                continue;

            newDamageValue = ChangeWoundDamage(wound.Value.AsNullable(), newDamageValue);

            if (!bodyPartDamage.DamageDict.TryAdd(damageType, newDamageValue))
                bodyPartDamage.DamageDict[damageType] += newDamageValue;
        }

        bodyPart.Comp3.Damage.ApplyDamage(bodyPartDamage);
        Damageable.DamageChanged(bodyPart, bodyPart.Comp3, bodyPartDamage);
        bodyPart.Comp2.WoundSeverity = bodyPart.Comp2.Thresholds.HighestMatch(bodyPart.Comp3.TotalDamage) ?? WoundSeverity.Healthy;

        foreach (var organSlot in bodyPart.Comp1.Organs.Values)
        {
            if (!TryComp<WoundableOrganComponent>(organSlot.OrganUid, out var woundableOrgan))
                continue;

            ApplyOrganDamage((organSlot.OrganUid.Value, null, woundableOrgan), bodyPartDamage);
        }

        return bodyPartDamage;
    }

    #endregion
}
