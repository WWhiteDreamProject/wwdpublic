using Content.Shared._White.Body.Systems;
using Content.Shared._White.Damage;
using Content.Shared._White.Damage.Components;
using Content.Shared._White.Damage.Systems;
using Content.Shared._White.Threshold;
using Content.Shared._White.Wounds.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Wounds.Systems;

public sealed partial class WoundableSystem
{
    private void InitializeAccumulator()
    {
        SubscribeLocalEvent<WoundableAccumulatorComponent, BeforeHandleDamageChangeEvent>(OnBeforeHandleDamageChange);
        SubscribeLocalEvent<WoundableAccumulatorComponent, BodyRelayedEvent<GetWoundableDamageEvent>>(OnGetWoundableDamage);
    }

    #region Event Handling

    private void OnBeforeHandleDamageChange(Entity<WoundableAccumulatorComponent> ent, ref BeforeHandleDamageChangeEvent args)
    {
        args.Handled = true;

        if (!TryChangeDamage((ent, ent.Comp, null), args.Damage, out var result, args.IgnoreResistances, args.InterruptsDoAfters, args.Origin))
            return;

        args.Result = result;
    }

    private void OnGetWoundableDamage(Entity<WoundableAccumulatorComponent> ent, ref BodyRelayedEvent<GetWoundableDamageEvent> args)
    {
        ChangeDamage((ent, ent.Comp, null), args.Args.Damage, args.Args.IgnoreResistances, origin: args.Args.Origin);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Attempts to change the damage dealt to given woundable accumulator.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="specifier">The original amount by which the damage must be changed.</param>
    /// <param name="result">The returned amount by which the damage has changed.</param>
    /// <param name="ignoreResistances">Determines whether the damage change should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <returns>True if the damage was successfully changed, false otherwise.</returns>
    public bool TryChangeDamage(
        Entity<WoundableAccumulatorComponent?, DamageableComponent?> ent,
        DamageSpecifier specifier,
        out DamageSpecifier result,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        result = ChangeDamage(ent, specifier, ignoreResistances, interruptsDoAfters, origin);
        return !result.Empty;
    }

    /// <summary>
    /// Attempts to change the damage dealt to given woundable accumulator.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="specifier">The original amount by which the damage must be changed.</param>
    /// <param name="ignoreResistances">Determines whether the damage change should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <returns>True if the damage was successfully changed, false otherwise.</returns>
    public bool TryChangeDamage(
        Entity<WoundableAccumulatorComponent?, DamageableComponent?> ent,
        DamageSpecifier specifier,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        return TryChangeDamage(ent, specifier, out _, ignoreResistances, interruptsDoAfters, origin);
    }

    /// <summary>
    /// Changes the damage dealt to given woundable accumulator.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="specifier">The original amount by which the damage must be changed.</param>
    /// <param name="ignoreResistances">Determines whether the damage change should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <returns>The amount by which the damage has changed.</returns>
    public DamageSpecifier ChangeDamage(
        Entity<WoundableAccumulatorComponent?, DamageableComponent?> ent,
        DamageSpecifier specifier,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
        )
    {
        var result = new DamageSpecifier();

        if (specifier.Empty)
            return result;

        if (!_accumulatorQuery.Resolve(ent, ref ent.Comp1) || !_damageableQuery.Resolve(ent, ref ent.Comp2))
            return result;

        if (!ignoreResistances)
            specifier = _damageable.GetModifiedDamage((ent, ent.Comp2), specifier, origin);

        if (specifier.Empty)
            return result;

        foreach (var (type, damage) in specifier)
        {
            if (damage == 0 || !_damageable.SupportsType((ent, ent.Comp2), type))
                continue;

            result.Add(type, damage);
        }

        if (result.Empty)
            return result;

        _damageable.ApplyDamage((ent, ent.Comp2), result, interruptsDoAfters, origin);

        ent.Comp1.Health = FixedPoint2.Clamp(ent.Comp1.Health - result.GetTotal(), FixedPoint2.Zero, ent.Comp1.MaximumHealth);
        DirtyField(ent, ent.Comp1, nameof(WoundableAccumulatorComponent.Health));

        UpdateSeverity((ent, ent.Comp1));

        RelayPositiveDamage(ent, ent.Comp1.Heir, ignoreResistances, result, origin);

        return result;
    }

    #endregion

    #region Private API

    public void UpdateSeverity(Entity<WoundableAccumulatorComponent> ent)
    {
        var severity = ent.Comp.Thresholds.HighestMatch(ent.Comp.Health) ?? WoundSeverity.Healthy;
        if (ent.Comp.Severity == severity)
            return;

        ent.Comp.Severity = severity;
        DirtyField(ent, ent.Comp, nameof(WoundableProviderComponent.Severity));

        var ev = new WoundSeverityChangedEvent(severity);
        RaiseLocalEvent(ent, ref ev);
    }

    #endregion
}
