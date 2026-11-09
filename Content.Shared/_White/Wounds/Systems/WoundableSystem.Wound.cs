using Content.Shared._White.Damage.Components;
using Content.Shared._White.Damage.Prototypes;
using Content.Shared._White.Damage.Systems;
using Content.Shared._White.Threshold;
using Content.Shared._White.Wounds.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Wounds.Systems;

public sealed partial class WoundableSystem
{
    private void InitializeWound()
    {
        SubscribeLocalEvent<WoundComponent, BeforeHandleDamageChangeEvent>(OnBeforeHandleDamageChange);
    }

    #region Event Handling

    private void OnBeforeHandleDamageChange(Entity<WoundComponent> ent, ref BeforeHandleDamageChangeEvent args)
    {
        args.Handled = true;

        if (!args.Damage.TryGetValue(ent.Comp.Type, out var damage))
            return;

        if (!TryChangeDamage((ent, ent.Comp, null), damage, out var result, args.IgnoreResistances, args.InterruptsDoAfters, args.Origin))
            return;

        args.Result = new(ent.Comp.Type, result);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Attempts to change the damage dealt to given wound.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="damage">The original amount by which the damage must be changed.</param>
    /// <param name="result">The returned amount by which the damage has changed.</param>
    /// <param name="ignoreResistances">Determines whether the damage change should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <returns>True if the damage was successfully changed, false otherwise.</returns>
    public bool TryChangeDamage(
        Entity<WoundComponent?, DamageableComponent?> ent,
        FixedPoint2 damage,
        out FixedPoint2 result,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        result = ChangeDamage(ent, damage, ignoreResistances, interruptsDoAfters, origin);
        return result != FixedPoint2.Zero;
    }

    /// <summary>
    /// Attempts to change the damage dealt to given wound.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="damage">The original amount by which the damage must be changed.</param>
    /// <param name="ignoreResistances">Determines whether the damage change should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <returns>True if the damage was successfully changed, false otherwise.</returns>
    public bool TryChangeDamage(
        Entity<WoundComponent?, DamageableComponent?> ent,
        FixedPoint2 damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        return TryChangeDamage(ent, damage, out _, ignoreResistances, interruptsDoAfters, origin);
    }

    /// <summary>
    /// Changes the damage dealt to the given wound.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="damage">The original amount by which the damage must be changed.</param>
    /// <param name="ignoreResistances">Determines whether the damage change should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <returns>The amount by which the damage has changed.</returns>
    public FixedPoint2 ChangeDamage(
        Entity<WoundComponent?, DamageableComponent?> ent,
        FixedPoint2 damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
        )
    {
        var result = FixedPoint2.Zero;
        if (!_woundQuery.Resolve(ent, ref ent.Comp1) || !_damageableQuery.Resolve(ent, ref ent.Comp2))
            return result;

        result = FixedPoint2.Max(ent.Comp1.Damage + damage, FixedPoint2.Zero) - ent.Comp1.Damage;

        if (result == FixedPoint2.Zero)
            return result;

        if (result > FixedPoint2.Zero)
        {
            ent.Comp1.WoundedAt = _gameTiming.CurTime;
            DirtyField(ent, ent.Comp1, nameof(WoundComponent.WoundedAt));
        }

        _damageable.ApplyDamage((ent, ent.Comp2), new(ent.Comp1.Type, result), interruptsDoAfters, origin);

        ent.Comp1.Damage += result;
        DirtyField(ent, ent.Comp1, nameof(WoundComponent.Damage));

        UpdateSeverity((ent, ent.Comp1));

        return result;
    }

    #endregion

    #region Private API

    private bool TryProcessWound(
        Entity<WoundableProviderComponent?> ent,
        ProtoId<DamageTypePrototype> type,
        FixedPoint2 damage,
        out FixedPoint2 appliedDamage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        appliedDamage = FixedPoint2.Zero;
        if (damage == FixedPoint2.Zero)
            return false;

        if (TryGetWound(ent, out var wound, type))
        {
            appliedDamage = ChangeDamage(wound.Value.AsNullable(), damage, ignoreResistances, interruptsDoAfters, origin);
            return true;
        }

        if (damage < FixedPoint2.Zero
            || !_prototype.TryIndex(type, out var typePrototype)
            || typePrototype.Wound is not {} woundId
            || !TryCreateWound(ent, woundId, damage, out _, ignoreResistances, interruptsDoAfters, origin))
            return false;

        appliedDamage = damage;
        return true;
    }

    private void UpdateSeverity(Entity<WoundComponent> ent)
    {
        var severity = ent.Comp.Thresholds.HighestMatch(ent.Comp.Damage) ?? WoundSeverity.Healthy;
        if (severity == ent.Comp.Severity)
            return;

        ent.Comp.Severity = severity;
        DirtyField(ent, ent.Comp, nameof(WoundComponent.Severity));

        var ev = new WoundSeverityChangedEvent(severity);
        RaiseLocalEvent(ent, ref ev);
    }

    #endregion
}
