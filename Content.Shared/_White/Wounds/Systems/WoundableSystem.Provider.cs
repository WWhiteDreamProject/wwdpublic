using Content.Shared._White.Body.Systems;
using Content.Shared._White.Damage.Components;
using Content.Shared._White.Damage.Systems;
using Content.Shared._White.Medical.Healing.Systems;
using Content.Shared._White.Threshold;
using Content.Shared._White.Wounds.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;

namespace Content.Shared._White.Wounds.Systems;

public sealed partial class WoundableSystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<WoundableProviderComponent, AttemptHandleDamageEvent>(OnAttemptHandleDamage);
        SubscribeLocalEvent<WoundableProviderComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<WoundableProviderComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<WoundableProviderComponent, BodyRelayedEvent<GetHealingTargetEvent>>(OnGetHealingTarget);
        SubscribeLocalEvent<WoundableProviderComponent, BodyRelayedEvent<GetWoundableDamageEvent>>(OnGetWoundableDamage);
        SubscribeLocalEvent<WoundableProviderComponent, ComponentInit>(OnInit);
    }

    #region Event Handling

    private void OnAttemptHandleDamage(Entity<WoundableProviderComponent> ent, ref AttemptHandleDamageEvent args)
    {
        args.Handled = true;

        if (!TryChangeDamage((ent, ent.Comp, null), args.Damage, out var result, args.IgnoreResistances, args.InterruptsDoAfters, args.Origin))
            return;

        if (ent.Comp.Body is { } body)
            _damageable.ApplyDamage(body, result, args.InterruptsDoAfters, args.Origin);

        args.Result = result;
    }

    private void OnGotInserted(Entity<WoundableProviderComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        if (!_woundableQuery.TryComp(args.Body, out var woundableComp))
            return;

        ent.Comp.Body = args.Body;
        DirtyField(ent, ent.Comp, nameof(WoundableProviderComponent.Body));

        foreach (var wound in ent.Comp.Wounds)
        {
            woundableComp.Wounds.Add(wound);
        }
        Dirty(args.Body, woundableComp);

        if (!_damageableQuery.TryComp(ent, out var damageableComp))
            return;

        _damageable.ApplyDamage(args.Body, damageableComp.Damage, false);
    }

    private void OnGotRemoved(Entity<WoundableProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        if (!_woundableQuery.TryComp(args.Body, out var woundableComp))
            return;

        ent.Comp.Body = null;
        DirtyField(ent, ent.Comp, nameof(WoundableProviderComponent.Body));

        foreach (var wound in ent.Comp.Wounds)
        {
            woundableComp.Wounds.Remove(wound);
        }
        Dirty(args.Body, woundableComp);

        if (!_damageableQuery.TryComp(ent, out var damageableComp))
            return;

        _damageable.ApplyDamage(args.Body, -damageableComp.Damage, false);
    }

    private void OnGetHealingTarget(Entity<WoundableProviderComponent> ent, ref BodyRelayedEvent<GetHealingTargetEvent> args)
    {
        args.Args.Handled = true;

        if (!_damageableQuery.TryComp(ent, out var damageableComp))
            return;

        if (args.Args.Healing.Comp.DamageContainers is not null &&
            damageableComp.DamageContainer is not null &&
            !args.Args.Healing.Comp.DamageContainers.Contains(damageableComp.DamageContainer.Value))
            return;

        if (!_damageable.HasDamage((ent, damageableComp), args.Args.Healing.Comp.Damage))
        {
            args.Args.Popup = Loc.GetString("medical-item-cant-use-on-provider", ("item", args.Args.Healing.Owner), ("provider", ent.Owner));
            return;
        }

        args.Args.Target = (ent, damageableComp);
    }

    private void OnGetWoundableDamage(Entity<WoundableProviderComponent> ent, ref BodyRelayedEvent<GetWoundableDamageEvent> args)
    {
        if (!TryChangeDamage((ent, ent.Comp, null), args.Args.Damage, out var result, args.Args.IgnoreResistances, origin: args.Args.Origin))
            return;

        foreach (var (type, damage) in result.DamageDict)
        {
            if (args.Args.Result.DamageDict.TryAdd(type, damage))
                continue;

            args.Args.Result.DamageDict[type] += damage;
        }
    }

    private void OnInit(Entity<WoundableProviderComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _container.EnsureContainer<Container>(ent.Owner, WoundsContainerId);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Applies damage to woundable provider.
    /// </summary>
    /// <returns>
    /// Returns true if damage was successfully applied to the target, otherwise returns false.
    /// </returns>
    public bool TryChangeDamage(
        Entity<WoundableProviderComponent?, DamageableComponent?> ent,
        DamageSpecifier damage,
        out DamageSpecifier result,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        result = ChangeDamage(ent, damage, ignoreResistances, interruptsDoAfters, origin);
        return !result.Empty;
    }

    /// <inheritdoc cref="TryChangeDamage(Entity{WoundableProviderComponent?, DamageableComponent?}, DamageSpecifier, out DamageSpecifier?, bool, bool, EntityUid?)"/>
    public bool TryChangeDamage(
        Entity<WoundableProviderComponent?, DamageableComponent?> ent,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        return TryChangeDamage(ent, damage, out _, ignoreResistances, interruptsDoAfters, origin);
    }

    /// <summary>
    /// Applies damage to woundable provider.
    /// </summary>
    /// <returns>
    /// The actual amount of damage taken, as a DamageSpecifier.
    /// </returns>
    public DamageSpecifier ChangeDamage(
        Entity<WoundableProviderComponent?, DamageableComponent?> ent,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
        )
    {
        var result = new DamageSpecifier();

        if (damage.Empty)
            return result;

        if (!_providerQuery.Resolve(ent, ref ent.Comp1) || !_damageableQuery.Resolve(ent, ref ent.Comp2))
            return result;

        if (!ignoreResistances)
        {
            var getResistanceEv = new GetWoundableResistanceEvent(damage, origin);
            RaiseLocalEvent(ent, getResistanceEv);

            damage = getResistanceEv.Damage;
        }

        if (damage.Empty)
            return result;

        foreach (var (type, value) in damage.DamageDict)
        {
            if (value == 0 || !ent.Comp2.Damage.DamageDict.ContainsKey(type))
                continue;

            var processed = value;
            if (!ignoreResistances && _prototype.TryIndex(ent.Comp2.DamageModifierSet, out var modifierSet))
            {
                var floatDamage = value.Float();
                if (modifierSet.FlatReduction.TryGetValue(type, out var reduction))
                    floatDamage = Math.Max(0f, floatDamage - reduction);

                if (modifierSet.Coefficients.TryGetValue(type, out var coefficient))
                    floatDamage *= coefficient;

                processed = FixedPoint2.New(floatDamage);
            }

            if (!TryProcessWound(ent.AsNullable(), type, processed, origin, out processed))
                continue;

            result.DamageDict.Add(type, processed);
        }

        if (result.Empty)
            return result;

        _damageable.ApplyDamage((ent, ent.Comp2), result, interruptsDoAfters, origin);

        UpdateWoundSeverity((ent, ent.Comp1, ent.Comp2));

        RaiseLocalEvent(ent, new WoundableDamageChangedEvent(origin, result));

        return result;
    }

    #endregion

    #region Private API

    public void UpdateWoundSeverity(Entity<WoundableProviderComponent, DamageableComponent> ent)
    {
        var severity = ent.Comp1.Thresholds.HighestMatch(ent.Comp2.TotalDamage) ?? WoundSeverity.Healthy;
        if (ent.Comp1.Severity == severity)
            return;

        ent.Comp1.Severity = severity;
        DirtyField(ent, ent.Comp1, nameof(WoundableProviderComponent.Severity));

        RaiseLocalEvent(ent, new WoundableSeverityChangedEvent(severity));
    }

    #endregion
}
