using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Body.Components;
using Content.Shared.DragDrop;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Body.Systems;

public abstract partial class SharedBodySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private ISawmill _sawmill = default!;

    private EntityQuery<BodyComponent> _bodyQuery;
    private EntityQuery<BodyProviderComponent> _providerQuery;

    /// <summary>
    /// Container ID prefix for any body provider.
    /// </summary>
    public const string ProviderSlotContainerIdPrefix = "body_provider_slot_";

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("body");

        SubscribeLocalEvent<BodyComponent, CanDragEvent>(OnCanDrag);
        SubscribeLocalEvent<BodyComponent, MapInitEvent>(OnMapInit);

        InitializeProvider();
        InitializeRelay();

        _bodyQuery = GetEntityQuery<BodyComponent>();
        _providerQuery = GetEntityQuery<BodyProviderComponent>();
    }

    #region Event Handling

    private void OnCanDrag(Entity<BodyComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnMapInit(Entity<BodyComponent> ent, ref MapInitEvent args)
    {
        SetupProvider(ent.Comp.RootProvider, ent, ent, ent.Comp.RootProviderId);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Attempts to attach a body provider to this body.
    /// </summary>
    /// <param name="ent">The entity to which the provider should be attached.</param>
    /// <param name="provider">The entity to attach.</param>
    /// <param name="id">Optional ID to search for a specific slot. If null, any suitable slot will be found.</param>
    /// <returns>True if the provider was successfully attached, false otherwise.</returns>
    public bool TryAttachProvider(Entity<BodyComponent?> ent, Entity<BodyProviderComponent?> provider, string? id = null)
    {
        if (!_bodyQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (!_providerQuery.Resolve(provider, ref provider.Comp))
            return false;

        if (!TryGetEmptyProviderSlot(ent, out var slot, provider.Comp.Type, id))
            return false;

        if (slot.ContainerSlot is null)
            return false;

        return _container.Insert(provider.Owner, slot.ContainerSlot);
    }

    /// <summary>
    /// Attempts to attach a body provider to this entity.
    /// </summary>
    /// <param name="uid">The entity to which the provider should be attached.</param>
    /// <param name="provider">The entity to attach.</param>
    /// <param name="id">ID to search for a specific slot. If null, any suitable slot will be found.</param>
    /// <returns>True if the provider was successfully attached, false otherwise.</returns>
    public bool TryAttachProvider(EntityUid uid, Entity<BodyProviderComponent?> provider, string? id = null)
    {
        if (_bodyQuery.TryComp(uid, out var bodyComp))
            return TryAttachProvider((uid, bodyComp), provider, id);

        if (_providerQuery.TryComp(uid, out var providerComp))
            return TryAttachProvider((uid, providerComp), provider, id);

        return false;
    }

    /// <summary>
    /// Attempts to create a new body provider slot within this body.
    /// </summary>
    /// <param name="ent">The entity in which to create the slot.</param>
    /// <param name="id">The unique ID for the new slot.</param>
    /// <param name="type">The type of provider that can be placed in this slot.</param>
    /// <returns>True if the slot was successfully created, false otherwise.</returns>
    public bool TryCreateProviderSlot(Entity<BodyComponent?> ent, string id, BodyProviderType type)
    {
        if (!_bodyQuery.Resolve(ent, ref ent.Comp) || !TryGetRootProvider(ent, out var provider))
            return false;

        var slot = new BodyProviderSlot
        {
            Type = type,
            ContainerSlot = _container.EnsureContainer<ContainerSlot>(provider.Value, GetProviderSlotContainerId(id)),
        };

        if (!ent.Comp.Providers.TryAdd((id, GetNetEntity(provider.Value)), slot))
            return false;

        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Attempts to create a new body provider slot within this entity.
    /// </summary>
    /// <param name="uid">The entity in which to create the slot.</param>
    /// <param name="id">The unique ID for the new slot.</param>
    /// <param name="type">The type of provider that can be placed in this slot.</param>
    /// <returns>True if the slot was successfully created, false otherwise.</returns>
    public bool TryCreateProviderSlot(EntityUid uid, string id, BodyProviderType type)
    {
        if (_bodyQuery.TryComp(uid, out var bodyComp))
            return TryCreateProviderSlot((uid, bodyComp), id, type);

        if (_providerQuery.TryComp(uid, out var providerComp))
            return TryCreateProviderSlot((uid, providerComp), id, type);

        return false;
    }

    /// <summary>
    /// Attempts to find an empty body provider slot within this body.
    /// </summary>
    /// <param name="ent">The entity to search within.</param>
    /// <param name="slot">The found empty slot. Will be null if no slot is found.</param>
    /// <param name="type">The type of provider the slot should match.</param>
    /// <param name="id">If specified, only a slot with this exact ID will be searched.</param>
    /// <returns>True if an empty slot was found, false otherwise.</returns>
    public bool TryGetEmptyProviderSlot(Entity<BodyComponent?> ent, [NotNullWhen(true)] out BodyProviderSlot? slot, BodyProviderType type = BodyProviderType.All, string? id = null)
    {
        slot = null;
        if (!_bodyQuery.Resolve(ent, ref ent.Comp))
            return false;

        foreach (var possibleSlot in GetProviderSlots(ent, type, id).Values)
        {
            if (possibleSlot.HasProvider)
                continue;

            slot = possibleSlot;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to find an empty body provider slot within this entity.
    /// </summary>
    /// <param name="uid">The entity to search within.</param>
    /// <param name="slot">The found empty slot. Will be null if no slot is found.</param>
    /// <param name="type">The type of provider the slot should match.</param>
    /// <param name="id">If specified, only a slot with this exact ID will be searched.</param>
    /// <returns>True if an empty slot was found, false otherwise.</returns>
    public bool TryGetEmptyProviderSlot(EntityUid uid, [NotNullWhen(true)] out BodyProviderSlot? slot, BodyProviderType type = BodyProviderType.All, string? id = null)
    {
        if (_bodyQuery.TryComp(uid, out var bodyComp))
            return TryGetEmptyProviderSlot((uid, bodyComp), out slot, type, id);

        if (_providerQuery.TryComp(uid, out var providerComp))
            return TryGetEmptyProviderSlot((uid, providerComp), out slot, type, id);

        slot = null;
        return false;
    }

    /// <summary>
    /// Attempts to get a list of all body provider associated with this body,
    /// </summary>
    /// <param name="ent">The entity to search within.</param>
    /// <param name="providers">A list of found providers.</param>
    /// <param name="type">Filter by provider type.</param>
    /// <returns>True if providers were found, false otherwise.</returns>
    public bool TryGetProviders(Entity<BodyComponent?> ent, out List<Entity<BodyProviderComponent>> providers, BodyProviderType type = BodyProviderType.All)
    {
        providers = GetProviders(ent, type);
        return providers.Count > 0;
    }

    /// <summary>
    /// Attempts to get a list of all body provider associated with this entity,
    /// </summary>
    /// <param name="uid">The entity to search within.</param>
    /// <param name="providers">A list of found providers.</param>
    /// <param name="type">Filter by provider type.</param>
    /// <returns>True if providers were found, false otherwise.</returns>
    public bool TryGetProviders(EntityUid uid, out List<Entity<BodyProviderComponent>> providers, BodyProviderType type = BodyProviderType.All)
    {
        providers = GetProviders(uid, type);
        return providers.Count > 0;
    }

    /// <summary>
    /// Attempts to get the root body provider for this body.
    /// </summary>
    /// <param name="ent">The entity to search within.</param>
    /// <param name="provider">The root body provider</param>
    /// <returns>True if the root provider was successfully retrieved, false otherwise.</returns>
    public bool TryGetRootProvider(Entity<BodyComponent?> ent, [NotNullWhen(true)] out Entity<BodyProviderComponent>? provider)
    {
        provider = null;

        if (!_bodyQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (!_providerQuery.TryComp(ent.Comp.RootProvider.ProviderUid, out var providerComp))
            return false;

        provider = (ent.Comp.RootProvider.ProviderUid.Value, providerComp);
        return true;
    }

    /// <summary>
    /// Retrieves all body provider slots associated with this body.
    /// </summary>
    /// <param name="comp">The <see cref="BodyComponent"/> to search within.</param>
    /// <returns>A dictionary of found slots.</returns>
    public Dictionary<string, BodyProviderSlot> GetProviderSlots(BodyComponent comp)
    {
        return GetProviderSlots(comp.RootProvider, comp.RootProviderId);
    }

    /// <summary>
    /// Retrieves all body provider slots within a given root body provider slot.
    /// </summary>
    /// <param name="rootSlot">The starting slot from which to begin the traversal.</param>
    /// <param name="rootId">The unique ID of the root slot.</param>
    /// <returns>A dictionary of found slots.</returns>
    public Dictionary<string, BodyProviderSlot> GetProviderSlots(BodyProviderSlot rootSlot, string rootId)
    {
        var slots = new Dictionary<string, BodyProviderSlot> { { rootId, rootSlot }, };

        foreach (var (id, slot) in rootSlot.Providers)
        {
            foreach (var (childId, childSlot) in GetProviderSlots(slot, id))
            {
                if (slots.TryAdd(childId, childSlot))
                    continue;

                _sawmill.Error($"Unable to retrieve {typeof(BodyProviderSlot)} with ID {childId}. A duplicate key was found.");
            }
        }

        return slots;
    }

    /// <summary>
    /// Retrieves all body provider slots associated with this body.
    /// </summary>
    /// <param name="ent">The entity to search within.</param>
    /// <param name="type">Filter by provider type.</param>
    /// <param name="id">Slot ID for exact matching.</param>
    /// <returns>A dictionary of found slots.</returns>
    public Dictionary<string, BodyProviderSlot> GetProviderSlots(Entity<BodyComponent?> ent, BodyProviderType type = BodyProviderType.All, string? id = null)
    {
        var slots = new Dictionary<string, BodyProviderSlot>();
        if (!_bodyQuery.Resolve(ent, ref ent.Comp))
            return slots;

        foreach (var (key, slot) in ent.Comp.Providers)
        {
            if (!type.HasFlag(slot.Type) || !string.IsNullOrEmpty(id) && key.Id != id)
                continue;

            slots.Add(key.Id, slot);
        }

        return slots;
    }

    /// <summary>
    /// Retrieves all body provider slots associated with this entity.
    /// </summary>
    /// <param name="uid">The entity to search within.</param>
    /// <param name="type">Filter by provider type.</param>
    /// <param name="id">Slot ID for exact matching.</param>
    /// <returns>A dictionary of found slots.</returns>
    public Dictionary<string, BodyProviderSlot> GetProviderSlots(EntityUid uid, BodyProviderType type = BodyProviderType.All, string? id = null)
    {
        if (_bodyQuery.TryComp(uid, out var bodyComp))
            return GetProviderSlots((uid, bodyComp), type, id);

        if (_providerQuery.TryComp(uid, out var providerComp))
            return GetProviderSlots((uid, providerComp), type, id);

        return new Dictionary<string, BodyProviderSlot>();
    }

    /// <summary>
    /// Retrieves all body providers associated with this body.
    /// </summary>
    /// <param name="ent">The entity to search within.</param>
    /// <param name="type">Filter by provider type.</param>
    /// <returns>A dictionary of found providers.</returns>
    public List<Entity<BodyProviderComponent>> GetProviders(Entity<BodyComponent?> ent, BodyProviderType type = BodyProviderType.All)
    {
        var providers = new List<Entity<BodyProviderComponent>>();

        if (!_bodyQuery.Resolve(ent, ref ent.Comp))
            return providers;

        foreach (var slot in ent.Comp.Providers.Values)
        {
            if (!_providerQuery.TryComp(slot.ProviderUid, out var providerComp) || !type.HasFlag(slot.Type))
                continue;

            providers.Add((slot.ProviderUid.Value, providerComp));
        }

        return providers;
    }

    /// <summary>
    /// Retrieves all body providers associated with this entity.
    /// </summary>
    /// <param name="uid">The entity to search within.</param>
    /// <param name="type">Filter by provider type.</param>
    /// <returns>A list of found providers.</returns>
    public List<Entity<BodyProviderComponent>> GetProviders(EntityUid uid, BodyProviderType type = BodyProviderType.All)
    {
        if (_bodyQuery.TryComp(uid, out var bodyComp))
            return GetProviders((uid, bodyComp), type);

        if (_providerQuery.TryComp(uid, out var providerComp))
            return GetProviders((uid, providerComp), type);

        return new List<Entity<BodyProviderComponent>>();
    }

    /// <summary>
    /// Retrieves all body provider prototype associated with this body.
    /// </summary>
    /// <param name="comp">The <see cref="BodyComponent"/> to search within.</param>
    /// <returns>A list of found providers.</returns>
    public List<EntProtoId> GetProviders(BodyComponent comp)
    {
        var providers = new List<EntProtoId>();
        foreach (var bodyProviderSlot in GetProviderSlots(comp).Values)
        {
            if (bodyProviderSlot.StartingProvider is not {} startingProvider)
                continue;

            providers.Add(startingProvider);
        }

        return providers;
    }

    /// <summary>
    /// Gets the container ID constant for the specified slot ID.
    /// </summary>
    /// <param name="id">The ID of the slot.</param>
    /// <returns>The container ID.</returns>
    public static string GetProviderSlotContainerId(string id)
    {
        return ProviderSlotContainerIdPrefix + id;
    }

    /// <summary>
    /// Gets the slot ID from the container ID.
    /// </summary>
    /// <param name="id">The container ID.</param>
    /// <returns>The slot ID. Returns an empty string if the container ID is invalid.</returns>
    public string GetProviderSlotId(string id)
    {
        var slotIndex = id.IndexOf(ProviderSlotContainerIdPrefix, StringComparison.Ordinal);

        return slotIndex < 0 ? string.Empty : id.Remove(slotIndex, ProviderSlotContainerIdPrefix.Length);
    }

    #endregion
}

/// <summary>
/// Event raised on body provider, when it is inserted into a body.
/// </summary>
/// <param name="Body">The entity into which the provider was inserted.</param>
/// <param name="Provider">The entity that was inserted.</param>
[ByRefEvent]
public readonly record struct BodyProviderGotInsertedEvent(Entity<BodyComponent> Body, Entity<BodyProviderComponent> Provider);

/// <summary>
/// Event raised on body provider, when it is inserted into a parent body provider.
/// </summary>
/// <param name="Parent">The entity into which the provider was inserted.</param>
/// <param name="Provider">The entity that was inserted.</param>
[ByRefEvent]
public readonly record struct BodyProviderGotInsertedIntoParentEvent(Entity<BodyProviderComponent> Parent, Entity<BodyProviderComponent> Provider);

/// <summary>
/// Event raised on body provider, when it is removed from a body.
/// </summary>
/// <param name="Body">The entity from which the provider was removed.</param>
/// <param name="Provider">The entity that was ejected.</param>
[ByRefEvent]
public readonly record struct BodyProviderGotRemovedEvent(Entity<BodyComponent> Body, Entity<BodyProviderComponent> Provider);

/// <summary>
/// Event raised on body provider, when it is removed from a parent body provider.
/// </summary>
/// <param name="Parent">The entity from which the provider was removed.</param>
/// <param name="Provider">The entity that was ejected.</param>
[ByRefEvent]
public readonly record struct BodyProviderGotRemovedFromParentEvent(Entity<BodyProviderComponent> Parent, Entity<BodyProviderComponent> Provider);

/// <summary>
/// Event raised on entity, when a body provider is inserted into it.
/// </summary>
/// <param name="Provider">The entity that was inserted.</param>
[ByRefEvent]
public readonly record struct BodyProviderInsertedIntoEvent(Entity<BodyProviderComponent> Provider);

/// <summary>
/// Event raised on entity, when a body provider is removed from it.
/// </summary>
/// <param name="Provider">The entity that was ejected.</param>
[ByRefEvent]
public readonly record struct BodyProviderRemovedFromEvent(Entity<BodyProviderComponent> Provider);
