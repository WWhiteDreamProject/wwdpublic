using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Body;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Damage;
using Content.Shared._White.Damage.Components;
using Content.Shared._White.Damage.Prototypes;
using Content.Shared._White.Damage.Systems;
using Content.Shared._White.Wounds.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._White.Wounds.Systems;

public sealed partial class WoundableSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private ISawmill _sawmill = default!;

    private EntityQuery<DamageableComponent> _damageableQuery;
    private EntityQuery<WoundableAccumulatorComponent> _accumulatorQuery;
    private EntityQuery<WoundableComponent> _woundableQuery;
    private EntityQuery<WoundableProviderComponent> _providerQuery;
    private EntityQuery<WoundComponent> _woundQuery;

    /// <summary>
    /// Container ID for any wound.
    /// </summary>
    private const string WoundsContainerId = "wounds";

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("woundable");

        SubscribeLocalEvent<WoundableComponent, BeforeHandleDamageChangeEvent>(OnBeforeHandleDamageChange);
        SubscribeLocalEvent<WoundableComponent, RejuvenateEvent>(OnRejuvenate);

        InitializeAccumulator();
        InitializeProvider();
        InitializeRelay();
        InitializeResist();
        InitializeWound();

        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _accumulatorQuery = GetEntityQuery<WoundableAccumulatorComponent>();
        _woundableQuery = GetEntityQuery<WoundableComponent>();
        _providerQuery = GetEntityQuery<WoundableProviderComponent>();
        _woundQuery = GetEntityQuery<WoundComponent>();
    }

    #region Event Handling

    private void OnBeforeHandleDamageChange(Entity<WoundableComponent> ent, ref BeforeHandleDamageChangeEvent args)
    {
        args.Handled = true;

        var ev = new GetWoundableDamageEvent(args.ProviderType, args.IgnoreResistances, args.Damage, args.Origin);
        RaiseLocalEvent(ent, ref ev);

        if (ev.Result.Empty)
            return;

        _damageable.ApplyDamage((ent, args.Damageable), ev.Result, args.InterruptsDoAfters, args.Origin);

        args.Result = ev.Result;
    }

    private void OnRejuvenate(Entity<WoundableComponent> ent, ref RejuvenateEvent args)
    {
        foreach (var wound in ent.Comp.Wounds)
        {
            Del(wound);
        }

        ent.Comp.Wounds.Clear();
        Dirty(ent, ent.Comp);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Attempts to change the damage dealt to given entity.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="specifier">The original amount by which the damage must be changed.</param>
    /// <param name="result">The returned amount by which the damage has changed.</param>
    /// <param name="ignoreResistances">Determines whether the damage change should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <returns>True if the damage was successfully changed, false otherwise.</returns>
    public bool TryChangeDamage(
        Entity<DamageableComponent?> ent,
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
    /// Attempts to change the damage dealt to given entity.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="specifier">The original amount by which the damage must be changed.</param>
    /// <param name="ignoreResistances">Determines whether the damage change should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <returns>True if the damage was successfully changed, false otherwise.</returns>
    public bool TryChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier specifier,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        return TryChangeDamage(ent, specifier, out _, ignoreResistances, interruptsDoAfters, origin);
    }

    /// <inheritdoc cref="TryChangeDamage(Entity{DamageableComponent?}, DamageSpecifier, out DamageSpecifier, bool, bool, EntityUid?)"/>
    public bool TryChangeDamage(
        EntityUid uid,
        DamageSpecifier specifier,
        out DamageSpecifier result,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        result = ChangeDamage(uid, specifier, ignoreResistances, interruptsDoAfters, origin);
        return !result.Empty;
    }

    /// <inheritdoc cref="TryChangeDamage(EntityUid, DamageSpecifier, out DamageSpecifier, bool, bool, EntityUid?)"/>
    public bool TryChangeDamage(
        EntityUid uid,
        DamageSpecifier specifier,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        return TryChangeDamage(uid, specifier, out _, ignoreResistances, interruptsDoAfters, origin);
    }

    /// <summary>
    /// Attempts to get the wound for given entity.
    /// </summary>
    /// <param name="uid">The entity to search within.</param>
    /// <param name="wound">The found wound.</param>
    /// <param name="type">Filter by damage type.</param>
    /// <returns>True if the wound was successfully retrieved, false otherwise.</returns>
    public bool TryGetWound(
        EntityUid uid,
        [NotNullWhen(true)] out Entity<WoundComponent>? wound,
        ProtoId<DamageTypePrototype>? type = null
    )
    {
        if (_woundableQuery.TryComp(uid, out var woundableComp))
            return TryGetWound((uid, woundableComp), out wound, type);

        if (_providerQuery.TryComp(uid, out var providerComp))
            return TryGetWound((uid, providerComp), out wound, type);

        wound = null;
        return false;
    }

    /// <summary>
    /// Attempts to get the wound for given woundable entity.
    /// </summary>
    /// <param name="ent">The woundable entity to search within.</param>
    /// <param name="wound">The found wound.</param>
    /// <param name="type">Filter by damage type.</param>
    /// <returns>True if the wound was successfully retrieved, false otherwise.</returns>
    public bool TryGetWound(
        Entity<WoundableComponent?> ent,
        [NotNullWhen(true)] out Entity<WoundComponent>? wound,
        ProtoId<DamageTypePrototype>? type = null
    )
    {
        wound = null;

        if (!_woundableQuery.Resolve(ent, ref ent.Comp))
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
    /// Changes the damage dealt to given entity.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="specifier">The original amount by which the damage must be changed.</param>
    /// <param name="ignoreResistances">Determines whether the damage change should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <returns>The amount by which the damage has changed.</returns>
    public DamageSpecifier ChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier specifier,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        if (_accumulatorQuery.TryComp(ent, out var accumulatorComp))
            return ChangeDamage((ent, accumulatorComp, ent.Comp), specifier, ignoreResistances, interruptsDoAfters, origin);

        if (_providerQuery.TryComp(ent, out var provideComp))
            return ChangeDamage((ent, provideComp, ent.Comp), specifier, ignoreResistances, interruptsDoAfters, origin);

        return new();
    }

    /// <inheritdoc cref="ChangeDamage(Entity{DamageableComponent?}, DamageSpecifier, bool, bool, EntityUid?)"/>
    public DamageSpecifier ChangeDamage(
        EntityUid uid,
        DamageSpecifier specifier,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        if (!_damageableQuery.TryComp(uid, out var damageableComp))
            return new();

        return ChangeDamage((uid, damageableComp), specifier, ignoreResistances, interruptsDoAfters, origin);
    }

    /// <summary>
    /// Retrieves all wounds associated with given entity.
    /// </summary>
    /// <param name="uid">The entity to search within.</param>
    /// <param name="type">Filter by damage type.</param>
    /// <returns>A list of found wounds.</returns>
    public List<Entity<WoundComponent>> GetWounds(EntityUid uid, ProtoId<DamageTypePrototype>? type = null)
    {
        if (_woundableQuery.TryComp(uid, out var woundableComp))
            return GetWounds((uid, woundableComp), type);

        if (_providerQuery.TryComp(uid, out var providerComp))
            return GetWounds((uid, providerComp), type);

        return new();
    }

    /// <summary>
    /// Retrieves all wounds associated with given woundable entity.
    /// </summary>
    /// <param name="ent">The woundable entity to search within.</param>
    /// <param name="type">Filter by damage type.</param>
    /// <returns>A list of found wounds.</returns>
    public List<Entity<WoundComponent>> GetWounds(
        Entity<WoundableComponent?> ent,
        ProtoId<DamageTypePrototype>? type = null
    )
    {
        var wounds = new List<Entity<WoundComponent>>();

        if (!_woundableQuery.Resolve(ent, ref ent.Comp))
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

    private void RelayPositiveDamage(EntityUid uid, BodyProviderType providerType, bool ignoreResistances, DamageSpecifier damage, EntityUid? origin)
    {
        var positiveDamage = new DamageSpecifier(damage);
        positiveDamage.RemoveNegative();

        if (positiveDamage.Empty)
            return;

        var ev = new GetWoundableDamageEvent(providerType, ignoreResistances, positiveDamage, origin);
        RaiseLocalEvent(uid, ref ev);
    }

    #endregion
}

/// <summary>
/// Event raised on an entity to get the damage change on its woundable provider.
/// </summary>
/// <param name="ProviderType">The body provider that should take damage.</param>
/// <param name="IgnoreResistances">Determines whether the damage change should ignore resistances.</param>
/// <param name="Damage">The amount by which the damage must be changed.</param>
/// <param name="Origin">The entity which caused the change in damage, if any.</param>
[ByRefEvent]
public record struct GetWoundableDamageEvent(BodyProviderType ProviderType, bool IgnoreResistances, DamageSpecifier Damage, EntityUid? Origin) : IBodyRelayEvent
{
    /// <summary>
    /// The body provider that is supposed to cause damage.
    /// </summary>
    public BodyProviderType ProviderType { get; } = ProviderType;

    /// <summary>
    /// The amount by which the damage has changed.
    /// </summary>
    public DamageSpecifier Result = new();
}

/// <summary>
/// Event raised on an entity after changing his wound severity.
/// </summary>
/// <param name="Severity">The new severity level.</param>
[ByRefEvent]
public record struct WoundSeverityChangedEvent(WoundSeverity Severity);

/// <summary>
/// Event raised on a wound after it's created.
/// </summary>
/// <param name="Wound">The component whose was created.</param>
[ByRefEvent]
public record struct WoundCreatedEvent(WoundComponent Wound);
