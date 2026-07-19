using System.Collections.Frozen;
using Content.Shared._White.Body;
using Content.Shared._White.Damage.Components;
using Content.Shared._White.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Radiation.Events;
using Content.Shared.Rejuvenate;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Damage.Systems;

public sealed class DamageableSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private FrozenDictionary<ProtoId<DamageContainerPrototype>, HashSet<ProtoId<DamageTypePrototype>>> _typesByContainer = default!;
    private FrozenDictionary<ProtoId<DamageGroupPrototype>, HashSet<ProtoId<DamageTypePrototype>>> _typesByGroup = default!;

    private EntityQuery<DamageableComponent> _damageableQuery;

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<DamageableComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<DamageableComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<DamageableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DamageableComponent, OnIrradiatedEvent>(OnIrradiated);
        SubscribeLocalEvent<DamageableComponent, RejuvenateEvent>(OnRejuvenate);

        CacheGroupPrototypes();
        CacheContainerPrototypes();

        _damageableQuery = GetEntityQuery<DamageableComponent>();
    }

    #region Event Handling

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<DamageGroupPrototype>())
            CacheGroupPrototypes();

        if (ev.WasModified<DamageContainerPrototype>())
            CacheContainerPrototypes();
    }

    private void OnGetState(Entity<DamageableComponent> ent, ref ComponentGetState args)
    {
        args.State = new DamageableComponentState(ent.Comp);
    }

    private void OnHandleState(Entity<DamageableComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not DamageableComponentState state)
            return;

        ent.Comp.Container = state.Container;
        ent.Comp.ModifierSet = state.ModifierSet;

        // Has the damage actually changed?
        var delta = state.Damage - ent.Comp.Damage;
        delta.TrimZeros();

        if (delta.Empty)
            return;

        ent.Comp.Damage = state.Damage;

        OnDamageChanged(ent, delta);
    }

    private void OnInit(Entity<DamageableComponent> ent, ref ComponentInit args)
    {
        ent.Comp.DamagePerGroup = ent.Comp.Damage.GetDamagePerGroup(this, _prototype);
        ent.Comp.TotalDamage = ent.Comp.Damage.GetTotal();
    }

    private void OnIrradiated(Entity<DamageableComponent> ent, ref OnIrradiatedEvent args)
    {
        var damage = FixedPoint2.New(args.TotalRads);

        DamageSpecifier specifier = new();
        foreach (var type in ent.Comp.RadiationDamageTypes)
        {
            specifier.Add(type, damage);
        }

        ChangeDamage(ent.Owner, specifier, interruptsDoAfters: false);
    }

    private void OnRejuvenate(Entity<DamageableComponent> ent, ref RejuvenateEvent args)
    {
        SetAllDamage(ent.AsNullable(), FixedPoint2.Zero);
    }

    #endregion

    #region Public AP

    /// <summary>
    /// Checks if the entity has any damage that overlaps with the specified damage types.
    /// </summary>
    /// <param name="ent">The entity to check for damage.</param>
    /// <param name="specifier">The damage specifier to compare against.</param>
    /// <returns>True if the entity has at least one overlapping damage type, false otherwise.</returns>
    public bool HasDamage(Entity<DamageableComponent?> ent, DamageSpecifier specifier)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp))
            return false;

        foreach (var type in ent.Comp.Damage.Keys)
        {
            if (!specifier.ContainsKey(type))
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Applies damage specified via a <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="DamageSpecifier"/> is effectively just a dictionary of damage types and damage values. This
    /// function just applies the container's resistances (unless otherwise specified) and then changes the
    /// stored damage data. Division of group damage into types is managed by <see cref="DamageSpecifier"/>.
    /// </remarks>
    /// <returns>
    /// If the attempt was successful or not.
    /// </returns>
    public bool TryChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier specifier,
        out DamageSpecifier newDamage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        BodyProviderType providerType = BodyProviderType.AllParts
    )
    {
        newDamage = ChangeDamage(ent, specifier, ignoreResistances, interruptsDoAfters, origin, providerType);
        return !newDamage.Empty;
    }

    /// <inheritdoc cref="TryChangeDamage(Entity{DamageableComponent?}, DamageSpecifier, out DamageSpecifier, bool, bool, EntityUid?, BodyProviderType)"/>
    public bool TryChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier specifier,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        BodyProviderType providerType = BodyProviderType.AllParts
    )
    {
        return TryChangeDamage(ent, specifier, out _, ignoreResistances, interruptsDoAfters, origin, providerType);
    }

    /// <summary>
    /// Applies damage specified via a <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <returns>
    /// The actual amount of damage taken, as a <see cref="DamageSpecifier"/>.
    /// </returns>
    public DamageSpecifier ApplyDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier specifier,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
        )
    {
        var result = new DamageSpecifier();

        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return result;

        foreach (var (type, damage) in specifier)
        {
            if (!SupportsType(ent.Comp.Container, type))
                continue;

            var oldDamage = ent.Comp.Damage.GetValueOrDefault(type);
            var newDamage = FixedPoint2.Max(FixedPoint2.Zero, oldDamage + damage);
            if (newDamage == oldDamage)
                continue;

            ent.Comp.Damage[type] = newDamage;
            result[type] = newDamage - oldDamage;
        }

        if (!result.Empty)
            OnDamageChanged((ent, ent.Comp), result, interruptsDoAfters, origin);

        return result;
    }

    /// <summary>
    /// Applies damage specified via a <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="DamageSpecifier"/> is effectively just a dictionary of damage types and damage values. This
    /// function just applies the container's resistances (unless otherwise specified) and then changes the
    /// stored damage data. Division of group damage into types is managed by <see cref="DamageSpecifier"/>.
    /// </remarks>
    /// <returns>
    /// The actual amount of damage taken, as a DamageSpecifier.
    /// </returns>
    public DamageSpecifier ChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier specifier,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        BodyProviderType providerType = BodyProviderType.AllParts
    )
    {
        if (specifier.Empty || !_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return new ();

        var beforeEv = new BeforeDamageChangedEvent(specifier, origin);
        RaiseLocalEvent(ent, ref beforeEv);

        if (beforeEv.Cancelled)
            return new ();

        var attemptHandleEv = new BeforeHandleDamageEvent(providerType, ignoreResistances, interruptsDoAfters, ent.Comp, specifier, origin);
        RaiseLocalEvent(ent, attemptHandleEv);

        if (attemptHandleEv.Handled)
            return attemptHandleEv.Damage;

        if (!ignoreResistances)
        {
            if (ent.Comp.ModifierSet != null && _prototype.TryIndex(ent.Comp.ModifierSet, out var modifierSet))
                specifier = DamageSpecifier.ApplyModifierSet(specifier, modifierSet);

            var ev = new DamageModifyEvent(specifier, origin);
            RaiseLocalEvent(ent, ev);
            specifier = ev.Damage;

            if (specifier.Empty)
                return new ();
        }

        return ApplyDamage(ent, specifier, interruptsDoAfters, origin);
    }

    /// <summary>
    /// Returns a <see cref="DamageSpecifier"/> with all positive damage to the entity.
    /// </summary>
    /// <param name="ent">entity with damage</param>
    public DamageSpecifier GetDamage(Entity<DamageableComponent?> ent)
    {
        var specifier = new DamageSpecifier();
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return specifier;

        foreach (var (type, damage) in ent.Comp.Damage)
        {
            if (damage <= FixedPoint2.Zero)
                continue;

            specifier.Add(type, damage);
        }

        return specifier;
    }

    /// <summary>
    /// Returns a set of <see cref="DamageTypePrototype"/> asociated with <see cref="DamageContainerPrototype"/>.
    /// </summary>
    public HashSet<ProtoId<DamageTypePrototype>> GetTypes(ProtoId<DamageContainerPrototype> container)
    {
        if (!_typesByContainer.TryGetValue(container, out var types))
            return new();

        return types;
    }

    /// <summary>
    /// Returns a set of <see cref="DamageTypePrototype"/> asociated with <see cref="DamageGroupPrototype"/>.
    /// </summary>
    public HashSet<ProtoId<DamageTypePrototype>> GetTypes(ProtoId<DamageGroupPrototype> group)
    {
        if (!_typesByGroup.TryGetValue(group, out var types))
            return new();

        return types;
    }

    /// <summary>
    /// Changes all damage types supported by a <see cref="DamageableComponent"/> by the specified value.
    /// </summary>
    public void ChangeAllDamage(Entity<DamageableComponent?> ent, FixedPoint2 damage)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        var result = new DamageSpecifier();
        foreach (var (type, oldDamage) in ent.Comp.Damage)
        {
            var newDamage = FixedPoint2.Max(FixedPoint2.Zero, oldDamage + damage);
            if (newDamage == oldDamage)
                continue;

            ent.Comp.Damage[type] = newDamage;
            result[type] = newDamage - oldDamage;
        }

        OnDamageChanged((ent, ent.Comp), result);
    }

    /// <summary>
    /// Sets all damage types supported by a <see cref="DamageableComponent"/> to the specified value.
    /// </summary>
    /// <remarks>
    /// Does nothing If the given damage value is negative.
    /// </remarks>
    public void SetAllDamage(
        Entity<DamageableComponent?> ent,
        FixedPoint2 damage,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
        )
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false) || damage < 0)
            return;

        var result = new DamageSpecifier();
        foreach (var (type, oldDamage) in ent.Comp.Damage)
        {
            ent.Comp.Damage[type] = damage;
            result[type] = damage - oldDamage;
        }

        OnDamageChanged((ent, ent.Comp), result, interruptsDoAfters, origin);
    }

    /// <summary>
    /// Directly sets the damage in a damageable component.
    /// This method keeps the damage types supported by the DamageContainerPrototype in the component.
    /// If a type is given in <paramref name="specifier"/>, but not supported then it will not be set.
    /// If a type is supported but not given in <paramref name="specifier"/> then it will be set to 0.
    /// </summary>
    /// <remarks>
    /// Useful for some unfriendly folk. Also ensures that cached values are updated and that damage changed
    /// event is raised.
    /// </remarks>
    public void SetDamage(Entity<DamageableComponent?> ent, DamageSpecifier specifier)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        var result = new DamageSpecifier();

        foreach (var (type, damage) in ent.Comp.Damage)
        {
            if (specifier.ContainsKey(type))
                continue;

            if (damage > 0)
                result[type] = -damage;

            ent.Comp.Damage[type] = FixedPoint2.Zero;
        }

        foreach (var (type, damage) in specifier)
        {
            if (!SupportsType(ent.Comp.Container, type))
                continue;

            var oldDamage = ent.Comp.Damage.GetValueOrDefault(type);
            result[type] = damage - oldDamage;
            ent.Comp.Damage[type] = damage;
        }

        OnDamageChanged((ent, ent.Comp), result);
    }

    /// <summary>
    /// Set's the damage modifier set prototype for this entity.
    /// </summary>
    /// <param name="ent">The entity we're setting the modifier set of.</param>
    /// <param name="modifierSet">The prototype we're setting.</param>
    public void SetModifierSet(Entity<DamageableComponent?> ent, ProtoId<DamageModifierSetPrototype>? modifierSet)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.ModifierSet = modifierSet;
        Dirty(ent);
    }

    #endregion

    #region Private AP

    /// <returns>If the damage container can take the given damage type</returns>
    private bool SupportsType(ProtoId<DamageContainerPrototype>? container, ProtoId<DamageTypePrototype> type)
    {
        if (container is null)
            return true;

        return _typesByContainer[container.Value].Contains(type);
    }

    private void CacheContainerPrototypes()
    {
        var types = new Dictionary<ProtoId<DamageContainerPrototype>, HashSet<ProtoId<DamageTypePrototype>>>();

        foreach (var container in _prototype.EnumeratePrototypes<DamageContainerPrototype>())
        {
            var set = types.GetValueOrDefault(container) ?? [];

            foreach (var type in container.Types)
            {
                set.Add(type);
            }

            foreach (var group in container.Groups)
            {
                foreach (var type in _typesByGroup[group])
                {
                    set.Add(type);
                }
            }

            types[container] = set;
        }

        _typesByContainer = types.ToFrozenDictionary();
    }

    private void CacheGroupPrototypes()
    {
        var types = new Dictionary<ProtoId<DamageGroupPrototype>, HashSet<ProtoId<DamageTypePrototype>>>();

        foreach (var type in _prototype.EnumeratePrototypes<DamageTypePrototype>())
        {
            var set = types.GetValueOrDefault(type.Group) ?? [];
            set.Add(type);
            types[type.Group] = set;
        }

        _typesByGroup = types.ToFrozenDictionary();
    }

    /// <summary>
    /// If the damage in a DamageableComponent was changed, this function should be called.
    /// </summary>
    /// <remarks>
    /// This updates cached damage information, flags the component as dirty, and raises damage changed event.
    /// The damage changed event is used by other systems, such as damage thresholds.
    /// </remarks>
    private void OnDamageChanged(
        Entity<DamageableComponent> ent,
        DamageSpecifier specifier,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        ent.Comp.DamagePerGroup = ent.Comp.Damage.GetDamagePerGroup(this, _prototype);
        ent.Comp.TotalDamage = ent.Comp.Damage.GetTotal();
        Dirty(ent);

        RaiseLocalEvent(ent, new DamageChangedEvent(ent.Comp, specifier, interruptsDoAfters, origin));
    }

    #endregion
}

/// <summary>
/// Event raised before damage is done, so stuff can cancel it if necessary.
/// </summary>
[ByRefEvent]
public record struct BeforeDamageChangedEvent(DamageSpecifier Damage, EntityUid? Origin = null, bool Cancelled = false);

/// <summary>
/// Event raised before damage is a handle.
/// </summary>
public sealed class BeforeHandleDamageEvent(BodyProviderType providerType, bool ignoreResistances, bool interruptsDoAfters, DamageableComponent damageable, DamageSpecifier damage, EntityUid? origin) : HandledEntityEventArgs
{
    /// <summary>
    /// Contains damage after processing.
    /// </summary>
    public DamageSpecifier Result = new();

    /// <summary>
    /// What body provider should take damage?
    /// </summary>
    public readonly BodyProviderType ProviderType = providerType;

    /// <summary>
    /// Should we ignore damage resistance?
    /// </summary>
    public readonly bool IgnoreResistances = ignoreResistances;

    /// <summary>
    /// Does this event interrupt DoAfters?
    /// </summary>
    public readonly bool InterruptsDoAfters = interruptsDoAfters;

    /// <summary>
    /// This is the component whose damage must be changed.
    /// </summary>
    public readonly DamageableComponent Damageable = damageable;

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
/// Event raised on an entity when damage is about to be dealt,
/// in case anything else needs to modify it other than the base
/// damageable component.
/// </summary>
public sealed class DamageChangedEvent : EntityEventArgs
{
    /// <summary>
    /// Was any of the damage change dealing damage, or was it all healing?
    /// </summary>
    public readonly bool DamageIncreased;

    /// <summary>
    /// Does this event interrupt DoAfters?
    /// Note: As provided in the constructor, this *does not* account for DamageIncreased.
    /// S written into the event, this *does* account for DamageIncreased.
    /// </summary>
    public readonly bool InterruptsDoAfters;

    /// <summary>
    /// This is the component whose damage was changed.
    /// </summary>
    /// <remarks>
    /// Given that nearly every component that cares about a change in the damage needs to know the
    /// current damage values, directly passing this information prevents a lot of duplicate
    /// Owner.TryGetComponent() calls.
    /// </remarks>
    public readonly DamageableComponent Damageable;

    /// <summary>
    /// The amount by which the damage has changed.
    /// </summary>
    public readonly DamageSpecifier Damage;

    /// <summary>
    /// Contains the entity which caused the change in damage if any was responsible.
    /// </summary>
    public readonly EntityUid? Origin;

    public DamageChangedEvent(
        DamageableComponent damageable,
        DamageSpecifier damage,
        bool interruptsDoAfters,
        EntityUid? origin
    )
    {
        Damageable = damageable;
        Damage = damage;
        Origin = origin;

        foreach (var damageChange in Damage.Values)
        {
            if (damageChange <= 0)
                continue;

            DamageIncreased = true;

            break;
        }

        InterruptsDoAfters = interruptsDoAfters && DamageIncreased;
    }
}

/// <summary>
/// Event raised on an entity when damage is about to be dealt,
/// in case anything else needs to modify it other than the base
/// damageable component.
/// </summary>
public sealed class DamageModifyEvent(DamageSpecifier damage, EntityUid? origin = null) : EntityEventArgs, IInventoryRelayEvent
{
    /// <summary>
    /// Contains the damage after modifiers have been applied.
    /// This is the damage that will be inflicted.
    /// </summary>
    public DamageSpecifier Result = damage;

    /// <remarks>
    /// Whenever locational damage is a thing, this should just check only that bit of armor.
    /// </remarks>
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;

    /// <summary>
    /// Contains the original damage, prior to any modifiers.
    /// </summary>
    public readonly DamageSpecifier Damage = damage;

    /// <summary>
    /// Contains the entity which caused the damage if any was responsible.
    /// </summary>
    public readonly EntityUid? Origin = origin;
}
