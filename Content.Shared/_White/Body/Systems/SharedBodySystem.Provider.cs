using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Body.Components;
using Robust.Shared.Containers;

namespace Content.Shared._White.Body.Systems;

public abstract partial class SharedBodySystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<BodyProviderComponent, EntGotInsertedIntoContainerMessage>(OnEntGotInsertedIntoContainer);
        SubscribeLocalEvent<BodyProviderComponent, EntGotRemovedFromContainerMessage>(OnEntGotRemovedFromContainer);
    }

    #region Event Handling

    private void OnEntGotInsertedIntoContainer(Entity<BodyProviderComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        var containerSlotId = args.Container.ID;
        if (containerSlotId.IndexOf(ProviderSlotContainerIdPrefix, StringComparison.Ordinal) == -1)
            return;

        var parent = args.Container.Owner;
        Entity<BodyComponent>? body = null;

        if (_bodyQuery.TryComp(parent, out var bodyComp))
            body = (parent, bodyComp);

        if (_providerQuery.TryComp(parent, out var providerComp))
        {
            ent.Comp.Parent = parent;
            DirtyField(ent, ent.Comp, nameof(BodyProviderComponent.Parent));

            var parentEv = new BodyProviderInsertedIntoEvent(ent);
            RaiseLocalEvent(parent, ref parentEv);

            var providerEv = new BodyProviderGotInsertedIntoParentEvent((parent, providerComp), ent);
            RaiseLocalEvent(ent, ref providerEv);

            if (!providerComp.Body.HasValue || !Resolve(providerComp.Body.Value, ref bodyComp))
                return;

            body = (providerComp.Body.Value, bodyComp);
        }

        if (!body.HasValue)
            return;

        ProviderInserted(ent, body.Value, parent);
    }

    private void OnEntGotRemovedFromContainer(Entity<BodyProviderComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        var containerSlotId = args.Container.ID;
        if (containerSlotId.IndexOf(ProviderSlotContainerIdPrefix, StringComparison.Ordinal) == -1)
            return;

        var parent = args.Container.Owner;
        Entity<BodyComponent>? body = null;

        if (_bodyQuery.TryComp(parent, out var bodyComp))
            body = (parent, bodyComp);

        if (_providerQuery.TryComp(parent, out var providerComp))
        {
            ent.Comp.Parent = null;
            DirtyField(ent, ent.Comp, nameof(BodyProviderComponent.Parent));

            var parentEv = new BodyProviderRemovedFromEvent(ent);
            RaiseLocalEvent(parent, ref parentEv);

            var providerEv = new BodyProviderGotRemovedFromParentEvent((parent, providerComp), ent);
            RaiseLocalEvent(ent, ref providerEv);

            if (!providerComp.Body.HasValue || !Resolve(providerComp.Body.Value, ref bodyComp))
                return;

            body = (providerComp.Body.Value, bodyComp);
        }

        if (!body.HasValue)
            return;

        ProviderRemoved(ent, body.Value, parent);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Attempts to attach a body provider to this body provider.
    /// </summary>
    /// <param name="ent">The entity to which the provider should be attached.</param>
    /// <param name="provider">The entity to attach.</param>
    /// <param name="id">Optional ID to search for a specific slot. If null, any suitable slot will be found.</param>
    /// <returns>True if the provider was successfully attached, false otherwise.</returns>
    public bool TryAttachProvider(Entity<BodyProviderComponent?> ent, Entity<BodyProviderComponent?> provider, string? id = null)
    {
        if (!_providerQuery.Resolve(ent, ref ent.Comp))
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
    /// Attempts to create a new body provider slot within this body provider.
    /// </summary>
    /// <param name="ent">The entity in which to create the slot.</param>
    /// <param name="id">The unique ID for the new slot.</param>
    /// <param name="type">The type of provider that can be placed in this slot.</param>
    /// <returns>True if the slot was successfully created, false otherwise.</returns>
    public bool TryCreateProviderSlot(Entity<BodyProviderComponent?> ent, string id, BodyProviderType type)
    {
        if (!_providerQuery.Resolve(ent, ref ent.Comp))
            return false;

        var slot = new BodyProviderSlot
        {
            Type = type,
            ContainerSlot = _container.EnsureContainer<ContainerSlot>(ent, GetProviderSlotContainerId(id)),
        };

        if (!ent.Comp.Providers.TryAdd(id, slot))
            return false;

        if (_bodyQuery.TryComp(ent.Comp.Body, out var bodyComp))
        {
            if (!bodyComp.Providers.TryAdd((id, GetNetEntity(ent)), slot))
            {
                ent.Comp.Providers.Remove(id);
                return false;
            }

            Dirty(ent.Comp.Body.Value, bodyComp);
        }

        DirtyField(ent, ent.Comp, nameof(BodyProviderComponent.Providers));
        return true;
    }

    /// <summary>
    /// Attempts to detach a body provider from its parent.
    /// </summary>
    /// <param name="ent">The entity  to detach.</param>
    /// <returns>True if the provider was successfully detached, false otherwise.</returns>
    public bool TryDetachProvider(Entity<BodyProviderComponent?> ent)
    {
        if (!_providerQuery.Resolve(ent, ref ent.Comp) || !ent.Comp.Parent.HasValue)
            return false;

        return _container.RemoveEntity(ent.Comp.Parent.Value, ent);
    }

    /// <summary>
    /// Attempts to find an empty body provider slot within this body provider.
    /// </summary>
    /// <param name="ent">The entity to search within.</param>
    /// <param name="slot">The found empty slot. Will be null if no slot is found.</param>
    /// <param name="type">The type of provider the slot should match.</param>
    /// <param name="id">If specified, only a slot with this exact ID will be searched.</param>
    /// <returns>True if an empty slot was found, false otherwise.</returns>
    public bool TryGetEmptyProviderSlot(Entity<BodyProviderComponent?> ent, [NotNullWhen(true)] out BodyProviderSlot? slot, BodyProviderType type = BodyProviderType.All, string? id = null)
    {
        slot = null;
        if (!_providerQuery.Resolve(ent, ref ent.Comp))
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
    /// Attempts to get a list of all body provider associated with this body provider,
    /// </summary>
    /// <param name="ent">The entity to search within.</param>
    /// <param name="providers">A list of found providers.</param>
    /// <param name="type">Filter by provider type.</param>
    /// <returns>True if providers were found, false otherwise.</returns>
    public bool TryGetProviders(Entity<BodyProviderComponent?> ent, out List<Entity<BodyProviderComponent>> providers, BodyProviderType type = BodyProviderType.All)
    {
        providers = GetProviders(ent, type);
        return providers.Count > 0;
    }

    /// <summary>
    /// Retrieves all body provider slots associated with this body provider
    /// </summary>
    /// <param name="ent">The entity to search within.</param>
    /// <param name="type">Filter by provider type.</param>
    /// <param name="id">Slot ID for exact matching.</param>
    /// <returns>A dictionary of found slots.</returns>
    public Dictionary<string, BodyProviderSlot> GetProviderSlots(Entity<BodyProviderComponent?> ent, BodyProviderType type = BodyProviderType.All, string? id = null)
    {
        var slots = new Dictionary<string, BodyProviderSlot>();
        if (!_providerQuery.Resolve(ent, ref ent.Comp))
            return slots;

        foreach (var (providerId, slot) in ent.Comp.Providers)
        {
            if (!type.HasFlag(slot.Type) || !string.IsNullOrEmpty(id) && providerId != id)
                continue;

            slots.Add(providerId, slot);
        }

        return slots;
    }

    /// <summary>
    /// Retrieves all body providers associated with this body provider.
    /// </summary>
    /// <param name="ent">The entity to search within.</param>
    /// <param name="type">Filter by provider type.</param>
    /// <returns>A dictionary of found providers.</returns>
    public List<Entity<BodyProviderComponent>> GetProviders(Entity<BodyProviderComponent?> ent, BodyProviderType type = BodyProviderType.All)
    {
        var providers = new List<Entity<BodyProviderComponent>>();

        if (!_providerQuery.Resolve(ent, ref ent.Comp))
            return providers;

        foreach (var slot in ent.Comp.Providers.Values)
        {
            if (!_providerQuery.TryComp(slot.ProviderUid, out var providerComp) || !type.HasFlag(providerComp.Type))
                continue;

            providers.Add((slot.ProviderUid.Value, providerComp));
        }

        return providers;
    }

    #endregion

    #region Private API

    private void ProviderInserted(Entity<BodyProviderComponent> ent, Entity<BodyComponent> body, EntityUid parent)
    {
        var bodyEv = new BodyProviderInsertedIntoEvent(ent);
        RaiseLocalEvent(body, ref bodyEv);

        var providerEv = new BodyProviderGotInsertedEvent(body, ent);
        RaiseLocalEvent(ent, ref providerEv);

        foreach (var (id, slot) in ent.Comp.Providers)
        {
            if (slot.ProviderUid is not {} provider || !_providerQuery.TryComp(provider, out var providerComp))
                continue;

            body.Comp.Providers.Add((id, GetNetEntity(parent)), slot);
            ProviderInserted((provider, providerComp), body, ent);
        }

        if (ent.Comp.Body == body)
            return;

        ent.Comp.Body = body;
        DirtyField(ent, ent.Comp, nameof(BodyProviderComponent.Body));
    }

    private void ProviderRemoved(Entity<BodyProviderComponent> ent, Entity<BodyComponent> body, EntityUid parent)
    {
        var bodyEv = new BodyProviderRemovedFromEvent(ent);
        RaiseLocalEvent(body, ref bodyEv);

        var providerEv = new BodyProviderGotRemovedEvent(body, ent);
        RaiseLocalEvent(ent, ref providerEv);

        foreach (var (id, slot) in ent.Comp.Providers)
        {
            if (slot.ProviderUid is not {} provider || !_providerQuery.TryComp(provider, out var providerComp))
                continue;

            body.Comp.Providers.Remove((id, GetNetEntity(parent)));
            ProviderRemoved((provider, providerComp), body, ent);
        }

        if (ent.Comp.Body == body)
            return;

        ent.Comp.Body = body;
        DirtyField(ent, ent.Comp, nameof(BodyProviderComponent.Body));
    }

    private void SetupProvider(BodyProviderSlot slot, Entity<BodyComponent> body, EntityUid parent, string id)
    {
        slot.ContainerSlot ??= _container.EnsureContainer<ContainerSlot>(parent, GetProviderSlotContainerId(id));

        body.Comp.Providers.Add((id, GetNetEntity(parent)), slot);

        SetupProvider(slot, body, parent);
    }

    private void SetupProvider(BodyProviderSlot slot, Entity<BodyComponent> body, EntityUid parent)
    {
        if (slot.ContainerSlot == null
            || slot.HasProvider
            || string.IsNullOrEmpty(slot.StartingProvider))
            return;

        // TODO: When RT#6192 is merged replace this all with TrySpawnInContainer...
        var provider = Spawn(slot.StartingProvider, _transform.GetMoverCoordinates(body));
        if (!_providerQuery.TryComp(provider, out var providerComp))
        {
            _sawmill.Error($"Body provider {ToPrettyString(provider)} does not have {typeof(BodyProviderComponent)}");
            QueueDel(provider);
            return;
        }

        if (!_container.Insert(provider, slot.ContainerSlot))
        {
            _sawmill.Error($"Couldn't insert {ToPrettyString(provider)} to {parent}");
            QueueDel(provider);
            return;
        }

        foreach (var (childId, childSlot) in providerComp.Providers)
        {
            SetupProvider(childSlot, body, provider, childId);
        }
    }

    #endregion
}
