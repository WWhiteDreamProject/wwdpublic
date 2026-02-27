using Content.Shared._White.Body;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Damage.Components;
using Content.Shared._White.Damage.Systems;
using Content.Shared._White.Wounds.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
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

        SubscribeLocalEvent<WoundableComponent, AttemptHandleDamageEvent>(OnAttemptHandleDamage);
        SubscribeLocalEvent<WoundableComponent, RejuvenateEvent>(OnRejuvenate);

        InitializeAccumulator();
        InitializeProvider();
        InitializeRelay();
        InitializeResist();

        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _accumulatorQuery = GetEntityQuery<WoundableAccumulatorComponent>();
        _woundableQuery = GetEntityQuery<WoundableComponent>();
        _providerQuery = GetEntityQuery<WoundableProviderComponent>();
        _woundQuery = GetEntityQuery<WoundComponent>();
    }

    #region Event Handling

    private void OnAttemptHandleDamage(Entity<WoundableComponent> ent, ref AttemptHandleDamageEvent args)
    {
        args.Handled = true;

        var getDamageEv = new GetWoundableDamageEvent(args.ProviderType, args.IgnoreResistances, args.Damage, args.Origin);
        RaiseLocalEvent(ent, getDamageEv);

        if (getDamageEv.Result.Empty)
            return;

        _damageable.ApplyDamage((ent, args.Damageable), getDamageEv.Result, args.InterruptsDoAfters, args.Origin);

        args.Result = getDamageEv.Result;
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
    /// Applies damage to woundable entity, like <see cref="WoundableAccumulatorComponent"/> or <see cref="WoundableProviderComponent"/>.
    /// </summary>
    /// <remarks>
    /// If you need change <see cref="WoundableComponent"/> damage use <see cref="DamageableSystem"/>
    /// </remarks>
    /// <returns>
    /// The actual amount of damage taken, as a DamageSpecifier.
    /// </returns>
    public DamageSpecifier ChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        if (_accumulatorQuery.TryComp(ent, out var accumulatorComp))
            return ChangeDamage((ent, accumulatorComp, ent.Comp), damage, ignoreResistances, interruptsDoAfters, origin);

        if (_providerQuery.TryComp(ent, out var provideComp))
            return ChangeDamage((ent, provideComp, ent.Comp), damage, ignoreResistances, interruptsDoAfters, origin);

        return new DamageSpecifier();
    }

    /// <inheritdoc cref="ChangeDamage(Entity{DamageableComponent?}, DamageSpecifier, bool, bool, EntityUid?)"/>
    public DamageSpecifier ChangeDamage(
        EntityUid uid,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        if (_accumulatorQuery.TryComp(uid, out var accumulatorComp))
            return ChangeDamage((uid, accumulatorComp, null), damage, ignoreResistances, interruptsDoAfters, origin);

        if (_providerQuery.TryComp(uid, out var provideComp))
            return ChangeDamage((uid, provideComp, null), damage, ignoreResistances, interruptsDoAfters, origin);

        return new DamageSpecifier();
    }

    /// <summary>
    /// Applies damage to woundable entity, like <see cref="WoundableAccumulatorComponent"/> or <see cref="WoundableProviderComponent"/>.
    /// </summary>
    /// <remarks>
    /// If you need change <see cref="WoundableComponent"/> damage use <see cref="DamageableSystem"/>
    /// </remarks>
    /// <returns>
    /// Returns true if damage was successfully applied to the target, otherwise returns false.
    /// </returns>
    public bool TryChangeDamage(
        Entity<DamageableComponent?> ent,
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

    /// <inheritdoc cref="TryChangeDamage(Entity{DamageableComponent?}, DamageSpecifier, out DamageSpecifier, bool, bool, EntityUid?)"/>
    public bool TryChangeDamage(
        EntityUid uid,
        DamageSpecifier damage,
        out DamageSpecifier result,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        result = ChangeDamage(uid, damage, ignoreResistances, interruptsDoAfters, origin);
        return !result.Empty;
    }

    /// <inheritdoc cref="TryChangeDamage(Entity{DamageableComponent?}, DamageSpecifier, out DamageSpecifier, bool, bool, EntityUid?)"/>
    public bool TryChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        return TryChangeDamage(ent, damage, out _, ignoreResistances, interruptsDoAfters, origin);
    }

    /// <inheritdoc cref="TryChangeDamage(EntityUid, DamageSpecifier, out DamageSpecifier, bool, bool, EntityUid?)"/>
    public bool TryChangeDamage(
        EntityUid uid,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        return TryChangeDamage(uid, damage, out _, ignoreResistances, interruptsDoAfters, origin);
    }

    #endregion
}

/// <summary>
/// Event raised on an entity to get the damage on its woundable provider.
/// </summary>
public sealed class GetWoundableDamageEvent(BodyProviderType type, bool ignoreResistances, DamageSpecifier damage, EntityUid? origin) : IBodyRelayEvent
{
    /// <summary>
    /// The body provider that is supposed to cause damage.
    /// </summary>
    public BodyProviderType Type { get; } = type;

    /// <summary>
    /// A result showing how much damage the body has received.
    /// </summary>
    public DamageSpecifier Result = new();

    /// <summary>
    /// Should we ignore damage resistance?
    /// </summary>
    public readonly bool IgnoreResistances = ignoreResistances;

    /// <summary>
    /// Damage this entity should receive.
    /// </summary>
    public readonly DamageSpecifier Damage = damage;

    /// <summary>
    /// Contains the entity which caused the change in damage if any was responsible.
    /// </summary>
    public readonly EntityUid? Origin = origin;
}

/// <summary>
/// Event raised on an entity to get the damage with resistance taken into account.
/// </summary>
public sealed class GetWoundableResistanceEvent(DamageSpecifier damage, EntityUid? origin)
{
    /// <summary>
    /// Damage this entity should receive.
    /// </summary>
    public DamageSpecifier Damage = damage;

    /// <summary>
    /// Contains the entity which caused the change in damage if any was responsible.
    /// </summary>
    public readonly EntityUid? Origin = origin;
}

/// <summary>
/// Event raised on woundable entity after changing his damage.
/// </summary>
/// <remarks>
/// Used to record damage to downstream providers. If you need to record damage changes, it's better to use <see cref="DamageChangedEvent"/>.
/// </remarks>
public sealed class WoundableDamageChangedEvent(EntityUid? origin, DamageSpecifier damage) : IBodyRelayEvent
{
    public BodyProviderType Type { get; } = ~BodyProviderType.Part;

    /// <summary>
    /// Contains the entity which caused the change in damage if any was responsible.
    /// </summary>
    public readonly EntityUid? Origin = origin;

    /// <summary>
    /// The amount by which the damage has changed.
    /// </summary>
    public readonly DamageSpecifier Damage = damage;
}

/// <summary>
/// Event raised on woundable entity after changing his severity.
/// </summary>
public record struct WoundableSeverityChangedEvent(WoundSeverity Severity);

/// <summary>
/// Event raised on a wound after changing his damage.
/// </summary>
public sealed class WoundDamageChangedEvent(EntityUid? origin, FixedPoint2 damage, WoundComponent wound)
{
    /// <summary>
    /// Contains the entity which caused the change in damage if any was responsible.
    /// </summary>
    public readonly EntityUid? Origin = origin;

    /// <summary>
    /// The amount by which the damage has changed.
    /// </summary>
    public readonly FixedPoint2 Damage = damage;

    /// <summary>
    /// This is the component whose damage was changed.
    /// </summary>
    public readonly WoundComponent Wound = wound;
}

/// <summary>
/// Event raised on a wound after changing his severity.
/// </summary>
public record struct WoundSeverityChangedEvent(WoundSeverity Severity);
