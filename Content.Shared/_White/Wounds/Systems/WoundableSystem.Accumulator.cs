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
        SubscribeLocalEvent<WoundableAccumulatorComponent, BeforeHandleDamageEvent>(OnAttemptHandleDamage);
        SubscribeLocalEvent<WoundableAccumulatorComponent, BodyRelayedEvent<GetWoundableDamageEvent>>(OnGetWoundableDamage);
        SubscribeLocalEvent<WoundableAccumulatorComponent, BodyRelayedEvent<WoundableDamageChangedEvent>>(OnWoundableDamageChanged);
    }

    #region Event Handling

    private void OnAttemptHandleDamage(Entity<WoundableAccumulatorComponent> ent, ref BeforeHandleDamageEvent args)
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

    private void OnWoundableDamageChanged(Entity<WoundableAccumulatorComponent> ent, ref BodyRelayedEvent<WoundableDamageChangedEvent> args)
    {
        if (!args.Args.Damage.AnyPositive())
            return;

        var damage = new DamageSpecifier(args.Args.Damage);
        damage.RemoveNegative();

        TryChangeDamage((ent, ent.Comp, null), damage, origin: args.Args.Origin);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Applies damage to woundable accumulator.
    /// </summary>
    /// <returns>
    /// Returns true if damage was successfully applied to the target, otherwise returns false.
    /// </returns>
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

    /// <inheritdoc cref="TryChangeDamage(Entity{WoundableProviderComponent?, DamageableComponent?}, DamageSpecifier, out DamageSpecifier, bool, bool, EntityUid?)"/>
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
    /// Applies damage to woundable accumulator.
    /// </summary>
    /// <returns>
    /// The actual amount of damage taken, as a DamageSpecifier.
    /// </returns>
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
        {
            var getResistanceEv = new GetWoundableResistanceEvent(specifier, origin);
            RaiseLocalEvent(ent, getResistanceEv);

            specifier = getResistanceEv.Damage;
        }

        if (specifier.Empty)
            return result;

        foreach (var (type, damage) in specifier)
        {
            if (damage == 0 || !ent.Comp2.Damage.ContainsKey(type))
                continue;

            if (ignoreResistances || !_prototype.TryIndex(ent.Comp2.ModifierSet, out var modifierSet))
            {
                result.Add(type, damage);
                continue;
            }

            var processedDamage = damage;

            if (modifierSet.FlatReduction.TryGetValue(type, out var reduction))
                processedDamage = FixedPoint2.Max(0f, processedDamage - reduction);

            if (modifierSet.Coefficients.TryGetValue(type, out var coefficient))
                processedDamage *= coefficient;

            result.Add(type, processedDamage);
        }

        if (result.Empty)
            return result;

        _damageable.ApplyDamage((ent, ent.Comp2), result, interruptsDoAfters, origin);

        ent.Comp1.Health = FixedPoint2.Clamp(ent.Comp1.Health - result.GetTotal(), FixedPoint2.Zero, ent.Comp1.MaximumHealth);
        DirtyField(ent, ent.Comp1, nameof(WoundableAccumulatorComponent.Health));

        UpdateWoundSeverity((ent, ent.Comp1));

        RaiseLocalEvent(ent, new WoundableDamageChangedEvent(origin, result));

        return result;
    }

    #endregion

    #region Private API

    public void UpdateWoundSeverity(Entity<WoundableAccumulatorComponent> ent)
    {
        var severity = ent.Comp.Thresholds.HighestMatch(ent.Comp.Health) ?? WoundSeverity.Healthy;
        if (ent.Comp.Severity == severity)
            return;

        ent.Comp.Severity = severity;
        DirtyField(ent, ent.Comp, nameof(WoundableProviderComponent.Severity));

        RaiseLocalEvent(ent, new WoundableSeverityChangedEvent(severity));
    }

    #endregion
}
