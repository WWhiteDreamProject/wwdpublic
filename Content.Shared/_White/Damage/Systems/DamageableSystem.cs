using System.Collections.Frozen;
using Content.Shared._White.Body;
using Content.Shared._White.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
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

    private FrozenDictionary<ProtoId<DamageContainerPrototype>, HashSet<ProtoId<DamageTypePrototype>>> _supportedTypesByContainer = default!;

    private EntityQuery<DamageableComponent> _damageableQuery;

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<DamageableComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<DamageableComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<DamageableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DamageableComponent, OnIrradiatedEvent>(OnIrradiated);
        SubscribeLocalEvent<DamageableComponent, RejuvenateEvent>(OnRejuvenate);

        _damageableQuery = GetEntityQuery<DamageableComponent>();
    }

    #region Event Handling

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (!ev.WasModified<DamageContainerPrototype>() && !ev.WasModified<DamageGroupPrototype>())
            return;

        CachePrototypes();
    }

    private void OnGetState(Entity<DamageableComponent> ent, ref ComponentGetState args)
    {
        args.State = new DamageableComponentState(ent.Comp);
    }

    private void OnHandleState(Entity<DamageableComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not DamageableComponentState state)
            return;

        ent.Comp.DamageContainer = state.DamageContainer;
        ent.Comp.DamageModifierSet = state.DamageModifierSet;

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
        ent.Comp.Damage.GetDamagePerGroup(_prototype, ent.Comp.DamagePerGroup);
        ent.Comp.TotalDamage = ent.Comp.Damage.GetTotal();
    }

    private void OnIrradiated(Entity<DamageableComponent> ent, ref OnIrradiatedEvent args)
    {
        var damageValue = FixedPoint2.New(args.TotalRads);

        // Radiation should really just be a damage group instead of a list of types.
        DamageSpecifier damage = new();
        foreach (var type in ent.Comp.RadiationDamageTypes)
        {
            damage.DamageDict.Add(type, damageValue);
        }

        ChangeDamage(ent.Owner, damage, interruptsDoAfters: false);
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
    /// <param name="damage">The damage specifier to compare against.</param>
    /// <returns>True if the entity has at least one overlapping damage type, false otherwise.</returns>
    public bool HasDamage(Entity<DamageableComponent?> ent, DamageSpecifier damage)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp))
            return false;

        foreach (var type in ent.Comp.Damage.DamageDict.Keys)
        {
            if (!damage.DamageDict.ContainsKey(type))
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
        DamageSpecifier damage,
        out DamageSpecifier newDamage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        BodyProviderType providerType = BodyProviderType.AllParts
    )
    {
        newDamage = ChangeDamage(ent, damage, ignoreResistances, interruptsDoAfters, origin, providerType);
        return !newDamage.Empty;
    }

    /// <inheritdoc cref="TryChangeDamage(Entity{DamageableComponent?}, DamageSpecifier, out DamageSpecifier, bool, bool, EntityUid?, BodyProviderType)"/>
    public bool TryChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        BodyProviderType providerType = BodyProviderType.AllParts
    )
    {
        return TryChangeDamage(ent, damage, out _, ignoreResistances, interruptsDoAfters, origin, providerType);
    }

    /// <summary>
    /// Applies damage specified via a <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    /// This function just applies damage. No more, no less.
    /// </remarks>
    /// <returns>
    /// The actual amount of damage taken, as a DamageSpecifier.
    /// </returns>
    public DamageSpecifier ApplyDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier damage,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
        )
    {
        var result = new DamageSpecifier();

        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return result;

        result.DamageDict.EnsureCapacity(damage.DamageDict.Count);

        var dict = ent.Comp.Damage.DamageDict;
        foreach (var (type, value) in damage.DamageDict)
        {
            if (!SupportsType(ent.Comp.DamageContainer, type))
                continue;

            var oldValue = dict.GetValueOrDefault(type);
            var newValue = FixedPoint2.Max(FixedPoint2.Zero, oldValue + value);
            if (newValue == oldValue)
                continue;

            dict[type] = newValue;
            result.DamageDict[type] = newValue - oldValue;
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
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        BodyProviderType providerType = BodyProviderType.AllParts
    )
    {
        if (damage.Empty || !_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return new ();

        var beforeEv = new BeforeDamageChangedEvent(damage, origin);
        RaiseLocalEvent(ent, ref beforeEv);

        if (beforeEv.Cancelled)
            return new ();

        var attemptHandleEv = new AttemptHandleDamageEvent(providerType, ignoreResistances, interruptsDoAfters, ent.Comp, damage, origin);
        RaiseLocalEvent(ent, attemptHandleEv);

        if (attemptHandleEv.Handled)
            return attemptHandleEv.Damage;

        if (!ignoreResistances)
        {
            if (ent.Comp.DamageModifierSet != null && _prototype.TryIndex(ent.Comp.DamageModifierSet, out var modifierSet))
                damage = DamageSpecifier.ApplyModifierSet(damage, modifierSet);

            var ev = new DamageModifyEvent(damage, origin);
            RaiseLocalEvent(ent, ev);
            damage = ev.Damage;

            if (damage.Empty)
                return new ();
        }

        return ApplyDamage(ent, damage, interruptsDoAfters, origin);
    }

    /// <summary>
    /// Returns a <see cref="DamageSpecifier"/> with all positive damage to the entity.
    /// </summary>
    /// <param name="ent">entity with damage</param>
    public DamageSpecifier GetDamage(Entity<DamageableComponent?> ent)
    {
        var damage = new DamageSpecifier();
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return damage;

        damage.DamageDict.EnsureCapacity(ent.Comp.Damage.DamageDict.Count);

        foreach (var (damageId, value) in ent.Comp.Damage.DamageDict)
        {
            if (value > FixedPoint2.Zero)
                damage.DamageDict.Add(damageId, value);
        }

        return damage;
    }

    /// <summary>
    /// Changes all damage types supported by a <see cref="DamageableComponent"/> by the specified value.
    /// </summary>
    public void ChangeAllDamage(Entity<DamageableComponent?> ent, FixedPoint2 value)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        var result = new DamageSpecifier();
        foreach (var (type, oldValue) in ent.Comp.Damage.DamageDict)
        {
            var newValue = FixedPoint2.Max(FixedPoint2.Zero, oldValue + value);
            if (newValue == oldValue)
                continue;

            ent.Comp.Damage.DamageDict[type] = newValue;
            result.DamageDict[type] = newValue - oldValue;
        }

        OnDamageChanged((ent, ent.Comp), result);
    }

    /// <summary>
    /// Sets all damage types supported by a <see cref="Components.DamageableComponent"/> to the specified value.
    /// </summary>
    /// <remarks>
    /// Does nothing If the given damage value is negative.
    /// </remarks>
    public void SetAllDamage(
        Entity<DamageableComponent?> ent,
        FixedPoint2 newValue,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
        )
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false) || newValue < 0)
            return;

        var result = new DamageSpecifier();
        foreach (var (type, oldValue) in ent.Comp.Damage.DamageDict)
        {
            ent.Comp.Damage.DamageDict[type] = newValue;
            result.DamageDict[type] = newValue - oldValue;
        }

        OnDamageChanged((ent, ent.Comp), result, interruptsDoAfters, origin);
    }

    /// <summary>
    /// Directly sets the damage in a damageable component.
    /// This method keeps the damage types supported by the DamageContainerPrototype in the component.
    /// If a type is given in <paramref name="damage"/>, but not supported then it will not be set.
    /// If a type is supported but not given in <paramref name="damage"/> then it will be set to 0.
    /// </summary>
    /// <remarks>
    /// Useful for some unfriendly folk. Also ensures that cached values are updated and that damage changed
    /// event is raised.
    /// </remarks>
    public void SetDamage(Entity<DamageableComponent?> ent, DamageSpecifier damage)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        var result = new DamageSpecifier();

        foreach (var (type, value) in ent.Comp.Damage.DamageDict)
        {
            if (damage.DamageDict.ContainsKey(type))
                continue;

            if (value > 0)
                result.DamageDict[type] = -value;

            ent.Comp.Damage.DamageDict[type] = FixedPoint2.Zero;
        }

        foreach (var (type, value) in damage.DamageDict)
        {
            if (!SupportsType(ent.Comp.DamageContainer, type))
                continue;

            var oldValue = ent.Comp.Damage.DamageDict.GetValueOrDefault(type);
            result.DamageDict[type] = value - oldValue;
            ent.Comp.Damage.DamageDict[type] = value;
        }

        OnDamageChanged((ent, ent.Comp), result);
    }

    /// <summary>
    /// Set's the damage modifier set prototype for this entity.
    /// </summary>
    /// <param name="ent">The entity we're setting the modifier set of.</param>
    /// <param name="damageModifierSet">The prototype we're setting.</param>
    public void SetDamageModifierSet(Entity<DamageableComponent?> ent, ProtoId<DamageModifierSetPrototype>? damageModifierSet)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.DamageModifierSet = damageModifierSet;
        Dirty(ent);
    }

    #endregion

    #region Private AP

    /// <returns>If the damage container can take the given damage type</returns>
    private bool SupportsType(ProtoId<DamageContainerPrototype>? container, ProtoId<DamageTypePrototype> type)
    {
        if (container is null)
            return true;

        return _supportedTypesByContainer[container.Value].Contains(type);
    }

    private void CachePrototypes()
    {
        var types = new Dictionary<ProtoId<DamageContainerPrototype>, HashSet<ProtoId<DamageTypePrototype>>>();

        foreach (var proto in _prototype.EnumeratePrototypes<DamageContainerPrototype>())
        {
            var set = new HashSet<ProtoId<DamageTypePrototype>>();
            types[proto.ID] = set;

            foreach (var type in proto.SupportedTypes)
            {
                set.Add(type);
            }

            foreach (var groupId in proto.SupportedGroups)
            {
                var group = _prototype.Index<DamageGroupPrototype>(groupId);
                foreach (var type in group.DamageTypes)
                {
                    set.Add(type);
                }
            }
        }

        _supportedTypesByContainer = types.ToFrozenDictionary();
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
        DamageSpecifier damage,
        bool interruptsDoAfters = true,
        EntityUid? origin = null
    )
    {
        ent.Comp.Damage.GetDamagePerGroup(_prototype, ent.Comp.DamagePerGroup);
        ent.Comp.TotalDamage = ent.Comp.Damage.GetTotal();
        Dirty(ent);

        RaiseLocalEvent(ent, new DamageChangedEvent(ent.Comp, damage, interruptsDoAfters, origin));
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
public sealed class AttemptHandleDamageEvent(BodyProviderType providerType, bool ignoreResistances, bool interruptsDoAfters, DamageableComponent damageable, DamageSpecifier damage, EntityUid? origin) : HandledEntityEventArgs
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

        foreach (var damageChange in Damage.DamageDict.Values)
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
