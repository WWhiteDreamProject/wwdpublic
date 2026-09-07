using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Damage;
using Content.Shared._White.Damage.Components;
using Content.Shared._White.Damage.Prototypes;
using Content.Shared._White.Damage.Systems;
using Content.Shared._White.Medical.Healing.Systems;
using Content.Shared._White.Threshold;
using Content.Shared._White.Wounds.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Wounds.Systems;

public sealed partial class WoundableSystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<WoundableProviderComponent, BeforeHandleDamageChangeEvent>(OnBeforeHandleDamageChange);
        SubscribeLocalEvent<WoundableProviderComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<WoundableProviderComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<WoundableProviderComponent, BodyRelayedEvent<GetHealingTargetEvent>>(OnGetHealingTarget);
        SubscribeLocalEvent<WoundableProviderComponent, BodyRelayedEvent<GetWoundableDamageEvent>>(OnGetWoundableDamage);
        SubscribeLocalEvent<WoundableProviderComponent, ComponentInit>(OnInit);
    }

    #region Event Handling

    private void OnBeforeHandleDamageChange(Entity<WoundableProviderComponent> ent, ref BeforeHandleDamageChangeEvent args)
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

        _damageable.ApplyDamage(args.Body.Owner, damageableComp.Damage, false);
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

        _damageable.ApplyDamage(args.Body.Owner, -damageableComp.Damage, false);
    }

    private void OnGetHealingTarget(Entity<WoundableProviderComponent> ent, ref BodyRelayedEvent<GetHealingTargetEvent> args)
    {
        args.Args.Handled = true;

        if (!_damageableQuery.TryComp(ent, out var damageableComp))
            return;

        if (args.Args.Healing.Comp.DamageContainers is not null &&
            damageableComp.Container is not null &&
            !args.Args.Healing.Comp.DamageContainers.Contains(damageableComp.Container.Value))
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

        foreach (var (type, damage) in result)
        {
            if (args.Args.Result.TryAdd(type, damage))
                continue;

            args.Args.Result[type] += damage;
        }
    }

    private void OnInit(Entity<WoundableProviderComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _container.EnsureContainer<Container>(ent.Owner, WoundsContainerId);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Attempts to change the damage dealt to given woundable provider.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="specifier">The original amount by which the damage must be changed.</param>
    /// <param name="result">The returned amount by which the damage has changed.</param>
    /// <param name="ignoreResistances">Determines whether the damage change should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <returns>True if the damage was successfully changed, false otherwise.</returns>
    public bool TryChangeDamage(
        Entity<WoundableProviderComponent?, DamageableComponent?> ent,
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
    /// Attempts to change the damage dealt to given woundable provider.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="specifier">The original amount by which the damage must be changed.</param>
    /// <param name="ignoreResistances">Determines whether the damage change should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <returns>True if the damage was successfully changed, false otherwise.</returns>
    public bool TryChangeDamage(
        Entity<WoundableProviderComponent?, DamageableComponent?> ent,
        DamageSpecifier specifier,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        return TryChangeDamage(ent, specifier, out _, ignoreResistances, interruptsDoAfters, origin);
    }

    /// <summary>
    /// Attempts to create a wound on woundable provider.
    /// </summary>
    /// <param name="ent">The woundable provider where the wound should be created.</param>
    /// <param name="woundId">The prototype ID of the wound to be created.</param>
    /// <param name="damage">The initial amount of damage the wound inflicts.</param>
    /// <param name="wound">Outputs the created wound entity.</param>
    /// <param name="ignoreResistances">Determines whether the damage change should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity that caused the wound.</param>
    /// <returns>True if the wound was successfully created, false otherwise.</returns>
    public bool TryCreateWound(
        Entity<WoundableProviderComponent?> ent,
        EntProtoId woundId,
        FixedPoint2 damage,
        [NotNullWhen(true)] out Entity<WoundComponent>? wound,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        wound = CreateWound(ent, woundId, damage, ignoreResistances, interruptsDoAfters, origin);
        return wound.HasValue;
    }

    /// <summary>
    /// Attempts to get the wound for given woundable provider.
    /// </summary>
    /// <param name="ent">The woundable entity to search within.</param>
    /// <param name="wound">The found wound.</param>
    /// <param name="type">Filter by damage type.</param>
    /// <returns>True if the wound was successfully retrieved, false otherwise.</returns>
    public bool TryGetWound(
        Entity<WoundableProviderComponent?> ent,
        [NotNullWhen(true)] out Entity<WoundComponent>? wound,
        ProtoId<DamageTypePrototype>? type = null
    )
    {
        wound = null;

        if (!_providerQuery.Resolve(ent, ref ent.Comp))
            return false;

        foreach (var providerWound in ent.Comp.Wounds)
        {
            if (!_woundQuery.TryComp(providerWound, out var woundComp)
                || !string.IsNullOrEmpty(type) && woundComp.Type != type)
                continue;

            wound = (providerWound, woundComp);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Changes the damage dealt to given woundable provider.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="specifier">The original amount by which the damage must be changed.</param>
    /// <param name="ignoreResistances">Determines whether the damage change should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <returns>The amount by which the damage has changed.</returns>
    public DamageSpecifier ChangeDamage(
        Entity<WoundableProviderComponent?, DamageableComponent?> ent,
        DamageSpecifier specifier,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
        )
    {
        var result = new DamageSpecifier();

        if (specifier.Empty)
            return result;

        if (!_providerQuery.Resolve(ent, ref ent.Comp1) || !_damageableQuery.Resolve(ent, ref ent.Comp2))
            return result;

        if (!ignoreResistances)
            specifier = _damageable.GetModifiedDamage((ent, ent.Comp2), specifier, origin);

        if (specifier.Empty)
            return result;

        foreach (var (type, damage) in specifier)
        {
            if (damage == 0 || !_damageable.SupportsType((ent, ent.Comp2), type))
                continue;

            if (!TryProcessWound(ent.AsNullable(), type, damage, out var appliedDamage, ignoreResistances, interruptsDoAfters, origin))
                continue;

            result.Add(type, appliedDamage);
        }

        if (result.Empty)
            return result;

        _damageable.ApplyDamage((ent, ent.Comp2), result, interruptsDoAfters, origin);

        UpdateSeverity((ent, ent.Comp1, ent.Comp2));

        RelayPositiveDamage(ent, ent.Comp1.Heir, ignoreResistances, result, origin);

        return result;
    }

    /// <summary>
    /// Creates a new wound on woundable provider.
    /// </summary>
    /// <param name="ent">The woundable provider where the wound should be created.</param>
    /// <param name="woundId">The prototype ID of the wound to be created.</param>
    /// <param name="damage">The initial amount of damage the wound inflicts.</param>
    /// <param name="ignoreResistances">Determines whether the wound create should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the wound create interrupts DoAfters.</param>
    /// <param name="origin">The entity that caused the wound.</param>
    /// <returns>Wound entity, or null if creation failed.</returns>
    public Entity<WoundComponent>? CreateWound(
        Entity<WoundableProviderComponent?> ent,
        EntProtoId woundId,
        FixedPoint2 damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        if (!_providerQuery.Resolve(ent, ref ent.Comp))
            return null;

        if (!TrySpawnInContainer(woundId, ent, WoundsContainerId, out var woundUid))
        {
            _sawmill.Error($"Couldn't insert wound '{woundId}' to {ent}");
            return null;
        }

        if (!_woundQuery.TryComp(woundUid, out var woundComponent))
        {
            _sawmill.Error($"Wound {ToPrettyString(woundUid)} does not have {typeof(WoundComponent)}");
            Del(woundUid);
            return null;
        }

        woundComponent.Body = ent.Comp.Body;
        DirtyField(woundUid.Value, woundComponent, nameof(WoundComponent.Body));

        woundComponent.Parent = ent;
        DirtyField(woundUid.Value, woundComponent, nameof(WoundComponent.Parent));

        woundComponent.WoundedAt = _gameTiming.CurTime;
        DirtyField(woundUid.Value, woundComponent, nameof(WoundComponent.WoundedAt));

        var ev = new WoundCreatedEvent(woundComponent);
        RaiseLocalEvent(woundUid.Value, ref ev);

        ChangeDamage((woundUid.Value, woundComponent, null), damage, ignoreResistances, interruptsDoAfters, origin);

        if (_woundableQuery.TryComp(ent.Comp.Body, out var woundableComp))
        {
            woundableComp.Wounds.Add(woundUid.Value);
            Dirty(ent.Comp.Body.Value, woundableComp);
        }

        return (woundUid.Value, woundComponent);
    }

    /// <summary>
    /// Retrieves all wounds associated with given woundable provider.
    /// </summary>
    /// <param name="ent">The woundable provider to search within.</param>
    /// <param name="type">Filter by damage type.</param>
    /// <returns>A list of found wounds.</returns>
    public List<Entity<WoundComponent>> GetWounds(
        Entity<WoundableProviderComponent?> ent,
        ProtoId<DamageTypePrototype>? type = null
    )
    {
        var wounds = new List<Entity<WoundComponent>>();

        if (!_providerQuery.Resolve(ent, ref ent.Comp))
            return wounds;

        foreach (var wound in ent.Comp.Wounds)
        {
            if (!_woundQuery.TryComp(wound, out var woundComp)
                || !string.IsNullOrEmpty(type) && woundComp.Type != type)
                continue;

            wounds.Add((wound, woundComp));
        }

        return wounds;
    }

    #endregion

    #region Private API

    public void UpdateSeverity(Entity<WoundableProviderComponent, DamageableComponent> ent)
    {
        var severity = ent.Comp1.Thresholds.HighestMatch(ent.Comp2.TotalDamage) ?? WoundSeverity.Healthy;
        if (ent.Comp1.Severity == severity)
            return;

        ent.Comp1.Severity = severity;
        DirtyField(ent, ent.Comp1, nameof(WoundableProviderComponent.Severity));

        var ev = new WoundSeverityChangedEvent(severity);
        RaiseLocalEvent(ent, ref ev);
    }

    #endregion
}
