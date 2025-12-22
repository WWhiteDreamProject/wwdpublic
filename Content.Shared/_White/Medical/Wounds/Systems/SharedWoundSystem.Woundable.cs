using Content.Shared._White.Body;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Medical.Wounds.Components.Woundable;
using Content.Shared._White.Threshold;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;
using Robust.Shared.Utility;

namespace Content.Shared._White.Medical.Wounds.Systems;

public abstract partial class SharedWoundSystem
{
    private void InitializeWoundable()
    {
        SubscribeLocalEvent<WoundableComponent, BodyPartAddedEvent>(OnBodyPartAdded);
        SubscribeLocalEvent<WoundableComponent, BodyPartRemovedEvent>(OnBodyPartRemoved);

        SubscribeLocalEvent<WoundableComponent, BeforeDamageCommitEvent>(OnBeforeDamageCommit);

        SubscribeLocalEvent<WoundableComponent, RejuvenateEvent>(OnRejuvenate);
    }

    #region Event Handling

    private void OnBodyPartAdded(Entity<WoundableComponent> woundable, ref BodyPartAddedEvent args)
    {
        if (!TryComp<DamageableComponent>(woundable, out var damageableBodyComponent)
            || !TryComp<DamageableComponent>(args.Part, out var damageableBodyPartComponent)
            || !TryComp<WoundableBodyPartComponent>(args.Part, out var woundableBodyPartComponent))
            return;

        woundableBodyPartComponent.Parent = woundable;

        damageableBodyComponent.Damage.ExclusiveAdd(damageableBodyPartComponent.Damage);
        Damageable.DamageChanged(woundable, damageableBodyComponent, damageableBodyPartComponent.Damage);

        woundable.Comp.Wounds[args.Part.Comp.Type] = woundableBodyPartComponent.Wounds;
    }

    private void OnBodyPartRemoved(Entity<WoundableComponent> woundable, ref BodyPartRemovedEvent args)
    {
        if (!TryComp<DamageableComponent>(woundable, out var damageableBodyComponent)
            || !TryComp<DamageableComponent>(args.Part, out var damageableBodyPartComponent)
            || !TryComp<WoundableBodyPartComponent>(args.Part, out var woundableBodyPartComponent))
            return;

        woundableBodyPartComponent.Parent = null;

        var damageDelta = -damageableBodyPartComponent.Damage;

        damageableBodyComponent.Damage.ExclusiveAdd(damageDelta);
        Damageable.DamageChanged(woundable, damageableBodyComponent, damageDelta);

        woundable.Comp.Wounds.Remove(args.Part.Comp.Type);
    }

    private void OnBeforeDamageCommit(Entity<WoundableComponent> woundable, ref BeforeDamageCommitEvent args)
    {
        if (!TryComp<DamageableComponent>(woundable, out var damageableBodyComponent))
            return;

        var bodyParts = _body.GetBodyParts<DamageableComponent>(woundable.Owner, args.BodyPartType);
        var damage = args.Damage / bodyParts.Count;
        var bodyDamage = new DamageSpecifier();

        foreach (var bodyPart in bodyParts)
        {
            if (!TryComp<WoundableBodyPartComponent>(bodyPart, out var woundableBodyPartComponent))
                continue;

            var bodyPartDamage = new DamageSpecifier();
            foreach (var (damageType, damageValue) in damage.DamageDict)
            {
                if (damageValue == 0
                    || !bodyPart.Comp2.Damage.DamageDict.ContainsKey(damageType)
                    || !_prototype.TryIndex<DamageTypePrototype>(damageType, out var damageTypePrototype))
                    continue;

                var newDamageValue = damageValue;
                if (!args.IgnoreResistances)
                {
                    if (_prototype.TryIndex(bodyPart.Comp2.DamageModifierSetId, out var modifierSet))
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

                    var damageModify = new DamageModifyEvent(modifyDamage, woundable, bodyPart.Comp1.Type);
                    RaiseLocalEvent(woundable, damageModify);

                    newDamageValue = damageModify.Damage[damageType];
                }

                var wound = GetWounds((bodyPart, woundableBodyPartComponent), damageType).FirstOrNull();
                if (!wound.HasValue
                    && (damageTypePrototype.Wound is not { } woundPrototype
                        || !TryCreateWound(
                            (bodyPart, bodyPart.Comp1, bodyPart.Comp2, woundableBodyPartComponent),
                            woundPrototype,
                            out wound,
                            (woundable, damageableBodyComponent, woundable.Comp))))
                    continue;

                newDamageValue = ChangeWoundDamage(wound.Value.AsNullable(), newDamageValue);

                if (!bodyPartDamage.DamageDict.TryAdd(damageType, newDamageValue))
                    bodyPartDamage.DamageDict[damageType] += newDamageValue;
            }

            bodyPart.Comp2.Damage.ApplyDamage(bodyPartDamage);
            Damageable.DamageChanged(bodyPart, bodyPart.Comp2, bodyPartDamage);
            woundableBodyPartComponent.WoundSeverity = woundableBodyPartComponent.Thresholds.HighestMatch(bodyPart.Comp2.TotalDamage) ?? WoundSeverity.Healthy;

            foreach (var (damageType, damageValue) in bodyPartDamage.DamageDict)
            {
                if (!bodyDamage.DamageDict.TryAdd(damageType, damageValue))
                    bodyDamage.DamageDict[damageType] += damageValue;
            }
        }

        if (bodyDamage.Empty)
            return;

        damageableBodyComponent.Damage.ApplyDamage(bodyDamage);
        Damageable.DamageChanged(woundable, damageableBodyComponent, bodyDamage);

        RaiseLocalEvent(woundable, new WoundableDamageChangedEvent(bodyDamage));

        args.Damage = bodyDamage;
        args.Handled = true;
    }

    private void OnRejuvenate(Entity<WoundableComponent> woundable, ref RejuvenateEvent args)
    {
        foreach (var wound in GetWounds(woundable.AsNullable()))
            PredictedQueueDel(wound.Owner);
    }

    #endregion
}
