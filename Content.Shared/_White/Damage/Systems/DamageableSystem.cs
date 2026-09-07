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
using Robust.Shared.Timing;

namespace Content.Shared._White.Damage.Systems;

public sealed class DamageableSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private FrozenDictionary<ProtoId<DamageContainerPrototype>, HashSet<ProtoId<DamageGroupPrototype>>> _groupsByContainer = default!;
    private FrozenDictionary<ProtoId<DamageContainerPrototype>, HashSet<ProtoId<DamageTypePrototype>>> _typesByContainer = default!;
    private FrozenDictionary<ProtoId<DamageGroupPrototype>, HashSet<ProtoId<DamageTypePrototype>>> _typesByGroup = default!;
    private FrozenDictionary<ProtoId<DamageTypePrototype>, HashSet<ProtoId<DamageGroupPrototype>>> _groupsByType = default!;

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
        ent.Comp.DamagePerGroup = ent.Comp.Damage.GetDamagePerGroup(_prototype);
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
    /// Attempts to change the damage dealt to given entity.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="specifier">The original amount by which the damage must be changed.</param>
    /// <param name="result">The returned amount by which the damage has changed.</param>
    /// <param name="ignoreResistances">Determines whether the damage change should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <param name="providerType">The body provider that should take damage.</param>
    /// <returns>True if the damage was successfully changed, false otherwise.</returns>
    public bool TryChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier specifier,
        out DamageSpecifier result,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        BodyProviderType providerType = BodyProviderType.AllParts
    )
    {
        result = ChangeDamage(ent, specifier, ignoreResistances, interruptsDoAfters, origin, providerType);
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
    /// <param name="providerType">The body provider that should take damage.</param>
    /// <returns>True if the damage was successfully changed, false otherwise.</returns>
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
    /// Determines whether the given entity can store a damage group.
    /// </summary>
    /// <param name="ent">The entity whose ability to store the damage group we want to determine.</param>
    /// <param name="group">The group of damage whose ability to be stored in entity we want to determine.</param>
    /// <returns>True, if the entity can take the given damage group, false otherwise.</returns>
    public bool SupportsGroup(Entity<DamageableComponent?> ent, ProtoId<DamageGroupPrototype> group)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return false;

        return SupportsGroup(ent.Comp.Container, group);
    }

    /// <summary>
    /// Determines whether the given damage container can store a damage group.
    /// </summary>
    /// <param name="container">The damage container whose ability to store the damage group we want to determine.</param>
    /// <param name="group">The group of damage whose ability to be stored in damage container we want to determine.</param>
    /// <returns>True, if the damage container can take the given damage group, false otherwise.</returns>
    public bool SupportsGroup(ProtoId<DamageContainerPrototype>? container, ProtoId<DamageGroupPrototype> group)
    {
        if (container is null)
            return true;

        return _groupsByContainer[container.Value].Contains(group);
    }

    /// <summary>
    /// Determines whether the given entity can store a damage type.
    /// </summary>
    /// <param name="ent">The entity whose ability to store the damage type we want to determine.</param>
    /// <param name="type">The type of damage whose ability to be stored in entity we want to determine.</param>
    /// <returns>True, if the entity can take the given damage type, false otherwise.</returns>
    public bool SupportsType(Entity<DamageableComponent?> ent, ProtoId<DamageTypePrototype> type)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return false;

        return SupportsType(ent.Comp.Container, type);
    }

    /// <summary>
    /// Determines whether the given damage container can store a damage type.
    /// </summary>
    /// <param name="container">The damage container whose ability to store the damage type we want to determine.</param>
    /// <param name="type">The type of damage whose ability to be stored in damage container we want to determine.</param>
    /// <returns>True, if the damage container can take the given damage type, false otherwise.</returns>
    public bool SupportsType(ProtoId<DamageContainerPrototype>? container, ProtoId<DamageTypePrototype> type)
    {
        if (container is null)
            return true;

        return _typesByContainer[container.Value].Contains(type);
    }

    /// <summary>
    /// Applies damage to the given entity.
    /// </summary>
    /// <remarks>Unlike <see cref="ChangeDamage"/>, it is not processed by any external handler.</remarks>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="specifier">The original amount by which the damage must be changed.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <returns>The amount by which the damage has changed.</returns>
    public DamageSpecifier ApplyDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier specifier,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
        )
    {
        var result = new DamageSpecifier();

        if (_gameTiming.ApplyingState || !_gameTiming.IsFirstTimePredicted)
            return result;

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
    /// Changes the damage dealt to given entity.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="specifier">The original amount by which the damage must be changed.</param>
    /// <param name="ignoreResistances">Determines whether the damage change should ignore resistances.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <param name="providerType">The body provider that should take damage.</param>
    /// <returns>The amount by which the damage has changed.</returns>
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
            return new();

        var beforeEv = new BeforeDamageChangedEvent(specifier, origin);
        RaiseLocalEvent(ent, ref beforeEv);

        if (beforeEv.Cancelled)
            return new();

        var beforeHandleEv = new BeforeHandleDamageChangeEvent(providerType, ignoreResistances, interruptsDoAfters, ent.Comp, specifier, origin);
        RaiseLocalEvent(ent, ref beforeHandleEv);

        if (beforeHandleEv.Handled)
            return beforeHandleEv.Result;

        if (!ignoreResistances)
            specifier = GetModifiedDamage(ent, specifier, origin);

        if (specifier.Empty)
            return new();

        return ApplyDamage(ent, specifier, interruptsDoAfters, origin);
    }

    /// <summary>
    /// Retrieves damage from the given entity.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to get.</param>
    /// <returns>The entity damage.</returns>
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
    /// Returns the modified damage for the given entity.
    /// </summary>
    /// <param name="ent">The entity whose modified damage we wish to get.</param>
    /// <param name="specifier">The original amount of damage.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
    /// <returns>The modified damage.</returns>
    public DamageSpecifier GetModifiedDamage(Entity<DamageableComponent?> ent, DamageSpecifier specifier, EntityUid? origin = null)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return specifier;

        if (_prototype.TryIndex(ent.Comp.ModifierSet, out var modifierSet))
            specifier = DamageSpecifier.ApplyModifierSet(specifier, modifierSet);

        var ev = new GetModifiedDamageEvent(specifier, origin);
        RaiseLocalEvent(ent, ref ev);
        specifier = ev.Result;

        return specifier;
    }

    /// <summary>
    /// Returns a set of <see cref="DamageGroupPrototype"/> associated with given <see cref="DamageTypePrototype"/>.
    /// </summary>
    /// <param name="type">The damage type whose supported groups we want to get.</param>
    /// <returns>A set of supported groups.</returns>
    public HashSet<ProtoId<DamageGroupPrototype>> GetGroup(ProtoId<DamageTypePrototype> type)
    {
        if (!_groupsByType.TryGetValue(type, out var groups))
            return new();

        return groups;
    }

    /// <summary>
    /// Returns a set of <see cref="DamageTypePrototype"/> associated with given <see cref="DamageContainerPrototype"/>.
    /// </summary>
    /// <param name="container">The damage container whose supported types we want to get.</param>
    /// <returns>A set of supported types.</returns>
    public HashSet<ProtoId<DamageTypePrototype>> GetTypes(ProtoId<DamageContainerPrototype> container)
    {
        if (!_typesByContainer.TryGetValue(container, out var types))
            return new();

        return types;
    }

    /// <summary>
    /// Changes all damage types supported by the given entity by the specified value.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="damage">Еhe value by which the damage changes.</param>
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
    /// Changes all damage types supported by the given entity to the specified value.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to change.</param>
    /// <param name="damage">Еhe value to which the damage changes.</param>
    /// <param name="interruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
    /// <param name="origin">The entity which caused the change in damage, if any.</param>
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
    /// Directly sets the damage dealt to given entity.
    /// </summary>
    /// <param name="ent">The entity whose damage we wish to set.</param>
    /// <param name="specifier">The amount to which the damage must be set.</param>
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
    /// Set's the damage modifier set prototype for given entity.
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

    private void CacheContainerPrototypes()
    {
        var groupsByContainer = new Dictionary<ProtoId<DamageContainerPrototype>, HashSet<ProtoId<DamageGroupPrototype>>>();
        var typesByContainer = new Dictionary<ProtoId<DamageContainerPrototype>, HashSet<ProtoId<DamageTypePrototype>>>();

        foreach (var container in _prototype.EnumeratePrototypes<DamageContainerPrototype>())
        {
            var groupsSet = groupsByContainer.GetValueOrDefault(container) ?? [];
            var typesSet = typesByContainer.GetValueOrDefault(container) ?? [];

            foreach (var (group, groupTypes) in _typesByGroup)
            {
                var containGroup = true;
                foreach (var type in groupTypes)
                {
                    if (container.Types.Contains(type))
                        continue;

                    containGroup = false;
                    break;
                }

                if (!containGroup)
                    continue;

                groupsSet.Add(group);
            }

            foreach (var type in container.Types)
            {
                typesSet.Add(type);
            }

            groupsByContainer[container] = groupsSet;
            typesByContainer[container] = typesSet;
        }

        _groupsByContainer = groupsByContainer.ToFrozenDictionary();
        _typesByContainer = typesByContainer.ToFrozenDictionary();
    }

    private void CacheGroupPrototypes()
    {
        var typesByGroup = new Dictionary<ProtoId<DamageGroupPrototype>, HashSet<ProtoId<DamageTypePrototype>>>();
        var groupsByType = new Dictionary<ProtoId<DamageTypePrototype>, HashSet<ProtoId<DamageGroupPrototype>>>();

        foreach (var group in _prototype.EnumeratePrototypes<DamageGroupPrototype>())
        {
            var typesSet = typesByGroup.GetValueOrDefault(group) ?? [];

            foreach (var type in group.Types)
            {
                var groupsSet = groupsByType.GetValueOrDefault(type) ?? [];

                typesSet.Add(type);
                groupsSet.Add(group);

                groupsByType[type] = groupsSet;
            }

            typesByGroup[group] = typesSet;
        }

        _typesByGroup = typesByGroup.ToFrozenDictionary();
        _groupsByType = groupsByType.ToFrozenDictionary();
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
        ent.Comp.DamagePerGroup = ent.Comp.Damage.GetDamagePerGroup(_prototype);
        ent.Comp.TotalDamage = ent.Comp.Damage.GetTotal();
        Dirty(ent);

        var ev = new DamageChangedEvent(interruptsDoAfters && specifier.AnyPositive(), ent.Comp, specifier, origin);
        RaiseLocalEvent(ent, ref ev);
    }

    #endregion
}

/// <summary>
/// Event raised on an entity before damage is changed.
/// </summary>
/// <param name="Damage">The amount by which the damage must be changed.</param>
/// <param name="Origin">The entity which caused the change in damage, if any.</param>
[ByRefEvent]
public record struct BeforeDamageChangedEvent(DamageSpecifier Damage, EntityUid? Origin)
{
    /// <summary>
    /// Determines whether a damage change was canceled by an external handler.
    /// </summary>
    public bool Cancelled = false;
}

/// <summary>
/// Event raised on an entity before damage change is a handle.
/// </summary>
/// <param name="ProviderType">The body provider that should take damage.</param>
/// <param name="IgnoreResistances">Determines whether the damage change should ignore resistances.</param>
/// <param name="InterruptsDoAfters">Determines whether the damage change interrupts DoAfters.</param>
/// <param name="Damageable">The component whose damage must be changed.</param>
/// <param name="Damage">The amount by which the damage must be changed.</param>
/// <param name="Origin">The entity which caused the change in damage, if any.</param>
[ByRefEvent]
public record struct BeforeHandleDamageChangeEvent(BodyProviderType ProviderType, bool IgnoreResistances, bool InterruptsDoAfters, DamageableComponent Damageable, DamageSpecifier Damage, EntityUid? Origin)
{
    /// <summary>
    /// Determines whether damage change was processed by an external damage handler.
    /// </summary>
    public bool Handled = false;

    /// <summary>
    /// The amount by which the damage has changed. Determined by the external damage handler.
    /// </summary>
    public DamageSpecifier Result = new();
}

/// <summary>
/// Event raised on an entity when damage is changed.
/// </summary>
/// <param name="InterruptsDoAfters">Determines whether the damage interrupts DoAfters.</param>
/// <param name="Damageable">The component whose damage was changed.</param>
/// <param name="Damage">The amount by which the damage has changed.</param>
/// <param name="Origin">The entity which caused the change in damage, if any.</param>
[ByRefEvent]
public record struct DamageChangedEvent(bool InterruptsDoAfters, DamageableComponent Damageable, DamageSpecifier Damage, EntityUid? Origin);

/// <summary>
/// Event raised on an entity to get the damage with modifiers taken into account.
/// </summary>
/// <param name="Damage">The original amount by which the damage must be changed.</param>
/// <param name="Origin">The entity which caused the change in damage, if any.</param>
[ByRefEvent]
public record struct GetModifiedDamageEvent(DamageSpecifier Damage, EntityUid? Origin) : IInventoryRelayEvent
{
    /// <summary>
    /// The modified amount by which the damage must be changed.
    /// </summary>
    public DamageSpecifier Result = Damage;

    /// <remarks>
    /// Whenever locational damage is a thing, this should just check only that bit of armor.
    /// </remarks>
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;
}
