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

        if (_providerQuery.TryComp(parent, out var parentProviderComp))
        {
            ent.Comp.Parent = parent;
            DirtyField(ent, ent.Comp, nameof(BodyProviderComponent.Parent));

            var parentEv = new BodyProviderInsertedIntoEvent(ent);
            RaiseLocalEvent(parent, ref parentEv);

            var providerEv = new BodyProviderGotInsertedIntoParentEvent(parent, ent.Comp);
            RaiseLocalEvent(ent, ref providerEv);

            if (!parentProviderComp.Body.HasValue || !Resolve(parentProviderComp.Body.Value, ref bodyComp))
                return;

            body = (parentProviderComp.Body.Value, bodyComp);
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

        if (_providerQuery.TryComp(parent, out var parentProviderComp))
        {
            ent.Comp.Parent = null;
            DirtyField(ent, ent.Comp, nameof(BodyProviderComponent.Parent));

            var parentEv = new BodyProviderGotRemovedEvent(ent, ent.Comp);
            RaiseLocalEvent(parent, ref parentEv);

            var providerEv = new BodyProviderGotRemovedFromParentEvent(parent, ent.Comp);
            RaiseLocalEvent(ent, ref providerEv);

            if (!parentProviderComp.Body.HasValue || !Resolve(parentProviderComp.Body.Value, ref bodyComp))
                return;

            body = (parentProviderComp.Body.Value, bodyComp);
        }

        if (!body.HasValue)
            return;

        ProviderRemoved(ent, body.Value, parent);
    }

    #endregion

    #region Private API

    private void ProviderInserted(Entity<BodyProviderComponent> ent, Entity<BodyComponent> body, EntityUid parent)
    {
        var bodyEv = new BodyProviderInsertedIntoEvent(ent);
        RaiseLocalEvent(body, ref bodyEv);

        var providerEv = new BodyProviderGotInsertedEvent(body, ent.Comp);
        RaiseLocalEvent(ent, ref providerEv);

        foreach (var (childProviderId, childProviderSlot) in ent.Comp.Providers)
        {
            if (childProviderSlot.ProviderUid is not {} childProvider || !_providerQuery.TryComp(childProvider, out var childProviderComp))
                continue;

            body.Comp.Providers.Add((childProviderId, GetNetEntity(parent)), childProviderSlot);
            ProviderInserted((childProvider, childProviderComp), body, ent);
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

        var providerEv = new BodyProviderGotRemovedEvent(body, ent.Comp);
        RaiseLocalEvent(ent, ref providerEv);

        foreach (var (childProviderId, childProviderSlot) in ent.Comp.Providers)
        {
            if (childProviderSlot.ProviderUid is not {} childProvider || !_providerQuery.TryComp(childProvider, out var childProviderComp))
                continue;

            body.Comp.Providers.Remove((childProviderId, GetNetEntity(parent)));
            ProviderRemoved((childProvider, childProviderComp), body, ent);
        }

        if (ent.Comp.Body == body)
            return;

        ent.Comp.Body = body;
        DirtyField(ent, ent.Comp, nameof(BodyProviderComponent.Body));
    }

    private void SetupProvider(BodyProviderSlot providerSlot, Entity<BodyComponent> body, EntityUid parent, string id)
    {
        providerSlot.ContainerSlot ??= _container.EnsureContainer<ContainerSlot>(parent, GetProviderSlotContainerId(id));

        body.Comp.Providers.Add((id, GetNetEntity(parent)), providerSlot);

        SetupProvider(providerSlot, body, parent);
    }

    private void SetupProvider(BodyProviderSlot providerSlot, Entity<BodyComponent> body, EntityUid parent)
    {
        if (providerSlot.ContainerSlot == null
            || providerSlot.HasProvider
            || string.IsNullOrEmpty(providerSlot.StartingProvider))
            return;

        // TODO: When RT#6192 is merged replace this all with TrySpawnInContainer...
        var provider = Spawn(providerSlot.StartingProvider, _transform.GetMoverCoordinates(body));
        if (!_providerQuery.TryComp(provider, out var providerComp))
        {
            _sawmill.Error($"Body provider {ToPrettyString(provider)} does not have {typeof(BodyProviderComponent)}");
            QueueDel(provider);
            return;
        }

        if (!_container.Insert(provider, providerSlot.ContainerSlot))
        {
            _sawmill.Error($"Couldn't insert {ToPrettyString(provider)} to {parent}");
            QueueDel(provider);
            return;
        }

        foreach (var (childProviderSlotId, childProviderSlot) in providerComp.Providers)
        {
            SetupProvider(childProviderSlot, body, provider, childProviderSlotId);
        }
    }

    #endregion

    #region Public API

    #region TryAttachProvider

    /// <summary>
    /// Trying to attach a body provider to this body.
    /// </summary>
    public bool TryAttachProvider(Entity<BodyComponent?> ent, Entity<BodyProviderComponent?> provider, string? id = null)
    {
        if (!_bodyQuery.Resolve(ent, ref ent.Comp)
            || !_providerQuery.Resolve(provider, ref provider.Comp)
            || !TryGetEmptyProviderSlot(ent, out var slot, provider.Comp.Type, id)
            || slot.ContainerSlot is null)
            return false;

        return _container.Insert(provider.Owner, slot.ContainerSlot);
    }

    /// <summary>
    /// Trying to attach a body provider to this body provider.
    /// </summary>
    public bool TryAttachProvider(Entity<BodyProviderComponent?> ent, Entity<BodyProviderComponent?> provider, string? id = null)
    {
        if (!_providerQuery.Resolve(ent, ref ent.Comp)
            || !_providerQuery.Resolve(provider, ref provider.Comp)
            || !TryGetEmptyProviderSlot(ent, out var slot, provider.Comp.Type, id)
            || slot.ContainerSlot is null)
            return false;

        return _container.Insert(provider.Owner, slot.ContainerSlot);
    }

    /// <summary>
    /// Trying to attach a body provider to this entity.
    /// </summary>
    public bool TryAttachProvider(EntityUid parent, Entity<BodyProviderComponent?> provider, string? id = null)
    {
        if (_bodyQuery.TryComp(parent, out var bodyComp))
            return TryAttachProvider((parent, bodyComp), provider, id);

        if (_providerQuery.TryComp(parent, out var providerComp))
            return TryAttachProvider((parent, providerComp), provider, id);

        return false;
    }

    #endregion

    #region TryCreateProviderSlot

    /// <summary>
    /// Trying to create a body provider slot for this body.
    /// </summary>
    public bool TryCreateProviderSlot(Entity<BodyComponent?> ent, string id, BodyProviderType type)
    {
        if (!_bodyQuery.Resolve(ent, ref ent.Comp) || !TryGetRootProvider(ent, out var provider))
            return false;

        var providerSlot = new BodyProviderSlot
        {
            Type = type,
            ContainerSlot = _container.EnsureContainer<ContainerSlot>(provider.Value, GetProviderSlotContainerId(id)),
        };

        if (!ent.Comp.Providers.TryAdd((id, GetNetEntity(provider.Value)), providerSlot))
            return false;

        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Trying to create a body provider slot for this body provider.
    /// </summary>
    public bool TryCreateProviderSlot(Entity<BodyProviderComponent?> ent, string id, BodyProviderType type)
    {
        if (!_providerQuery.Resolve(ent, ref ent.Comp))
            return false;

        var providerSlot = new BodyProviderSlot
        {
            Type = type,
            ContainerSlot = _container.EnsureContainer<ContainerSlot>(ent, GetProviderSlotContainerId(id)),
        };

        if (!ent.Comp.Providers.TryAdd(id, providerSlot))
            return false;

        if (_bodyQuery.TryComp(ent.Comp.Body, out var bodyComp))
        {
            if (!bodyComp.Providers.TryAdd((id, GetNetEntity(ent)), providerSlot))
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
    /// Trying to create an entity slot for this body part.
    /// </summary>
    public bool TryCreateProviderSlot(EntityUid parent, string id, BodyProviderType type)
    {
        if (_bodyQuery.TryComp(parent, out var bodyComp))
            return TryCreateProviderSlot((parent, bodyComp), id, type);

        if (_providerQuery.TryComp(parent, out var providerComp))
            return TryCreateProviderSlot((parent, providerComp), id, type);

        return false;
    }

    #endregion

    /// <summary>
    /// Trying to detach a body provider.
    /// </summary>
    public bool TryDetachProvider(Entity<BodyProviderComponent?> ent)
    {
        if (!_providerQuery.Resolve(ent, ref ent.Comp) || !ent.Comp.Parent.HasValue)
            return false;

        return _container.RemoveEntity(ent.Comp.Parent.Value, ent);
    }

    #region TryGetEmptyProviderSlot

    /// <summary>
    /// Trying to get the empty body provider slot for this body.
    /// </summary>
    public bool TryGetEmptyProviderSlot(Entity<BodyComponent?> ent, [NotNullWhen(true)] out BodyProviderSlot? slot, BodyProviderType type = BodyProviderType.All, string? id = null)
    {
        slot = null;
        if (!_bodyQuery.Resolve(ent, ref ent.Comp))
            return false;

        foreach (var providerSlot in GetProviderSlots(ent, type, id))
        {
            if (providerSlot.HasProvider)
                continue;

            slot = providerSlot;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Trying to get the empty body provider slot for this body provider.
    /// </summary>
    public bool TryGetEmptyProviderSlot(Entity<BodyProviderComponent?> ent, [NotNullWhen(true)] out BodyProviderSlot? slot, BodyProviderType type = BodyProviderType.All, string? id = null)
    {
        slot = null;
        if (!_providerQuery.Resolve(ent, ref ent.Comp))
            return false;

        foreach (var providerSlot in GetProviderSlots(ent, type, id))
        {
            if (providerSlot.HasProvider)
                continue;

            slot = providerSlot;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Trying to get the empty body provider slot for this entity.
    /// </summary>
    public bool TryGetEmptyProviderSlot(EntityUid parent, [NotNullWhen(true)] out BodyProviderSlot? slot, BodyProviderType type = BodyProviderType.All, string? id = null)
    {
        if (_bodyQuery.TryComp(parent, out var bodyComp))
            return TryGetEmptyProviderSlot((parent, bodyComp), out slot, type, id);

        if (_providerQuery.TryComp(parent, out var providerComp))
            return TryGetEmptyProviderSlot((parent, providerComp), out slot, type, id);

        slot = null;
        return false;
    }

    #endregion

    #region TryGetProviders

    /// <summary>
    /// Trying to get the body providers of this body.
    /// </summary>
    public bool TryGetProviders(Entity<BodyComponent?> ent, out List<Entity<BodyProviderComponent>> providers, BodyProviderType type = BodyProviderType.All)
    {
        providers = GetProviders(ent, type);
        return providers.Count > 0;
    }

    /// <summary>
    /// Trying to get the body providers of this provider.
    /// </summary>
    public bool TryGetProviders(Entity<BodyProviderComponent?> ent, out List<Entity<BodyProviderComponent>> providers, BodyProviderType type = BodyProviderType.All)
    {
        providers = GetProviders(ent, type);
        return providers.Count > 0;
    }

    /// <summary>
    /// Trying to get the body providers of this entity.
    /// </summary>
    public bool TryGetProviders(EntityUid parent, out List<Entity<BodyProviderComponent>> providers, BodyProviderType type = BodyProviderType.All)
    {
        providers = GetProviders(parent, type);
        return providers.Count > 0;
    }

    #endregion

    /// <summary>
    /// Trying to get the root body provider of this body.
    /// </summary>
    public bool TryGetRootProvider(Entity<BodyComponent?> ent, [NotNullWhen(true)] out Entity<BodyProviderComponent>? provider)
    {
        provider = null;

        if (!_bodyQuery.Resolve(ent, ref ent.Comp)
            || !_providerQuery.TryComp(ent.Comp.RootProvider.ProviderUid, out var providerComp))
            return false;

        provider = (ent.Comp.RootProvider.ProviderUid.Value, providerComp);
        return true;
    }

    #region GetProviders

    /// <summary>
    /// Gets the body providers of this body.
    /// </summary>
    public List<Entity<BodyProviderComponent>> GetProviders(Entity<BodyComponent?> ent, BodyProviderType type = BodyProviderType.All)
    {
        var providers = new List<Entity<BodyProviderComponent>>();

        if (!_bodyQuery.Resolve(ent, ref ent.Comp))
            return providers;

        foreach (var providerSlot in ent.Comp.Providers.Values)
        {
            if (!_providerQuery.TryComp(providerSlot.ProviderUid, out var providerComp)
                || !type.HasFlag(providerSlot.Type))
                continue;

            providers.Add((providerSlot.ProviderUid.Value, providerComp));
        }

        return providers;
    }

    /// <summary>
    /// Gets the body providers of this body provider.
    /// </summary>
    public List<Entity<BodyProviderComponent>> GetProviders(Entity<BodyProviderComponent?> ent, BodyProviderType type = BodyProviderType.All)
    {
        var providers = new List<Entity<BodyProviderComponent>>();

        if (!_providerQuery.Resolve(ent, ref ent.Comp))
            return providers;

        foreach (var providerSlot in ent.Comp.Providers.Values)
        {
            if (!_providerQuery.TryComp(providerSlot.ProviderUid, out var providerComp)
                || !type.HasFlag(providerComp.Type))
                continue;

            providers.Add((providerSlot.ProviderUid.Value, providerComp));
        }

        return providers;
    }

    /// <summary>
    ///  Gets the body providers of this entity.
    /// </summary>
    public List<Entity<BodyProviderComponent>> GetProviders(EntityUid parent, BodyProviderType type = BodyProviderType.All)
    {
        if (_bodyQuery.TryComp(parent, out var bodyComp))
            return GetProviders((parent, bodyComp), type);

        if (_providerQuery.TryComp(parent, out var providerComp))
            return GetProviders((parent, providerComp), type);

        return new List<Entity<BodyProviderComponent>>();
    }

    #endregion

    #region GetProviderSlots

    /// <summary>
    /// Gets the body provider slots of this body.
    /// </summary>
    public List<BodyProviderSlot> GetProviderSlots(Entity<BodyComponent?> ent, BodyProviderType type = BodyProviderType.All, string? id = null)
    {
        if (!_bodyQuery.Resolve(ent, ref ent.Comp))
            return new List<BodyProviderSlot>();

        var providerSlots = new List<BodyProviderSlot>();
        foreach (var providerSlot in ent.Comp.Providers.Values)
        {
            if (!type.HasFlag(providerSlot.Type) || !string.IsNullOrEmpty(id) && providerSlot.Id != id)
                continue;

            providerSlots.Add(providerSlot);
        }

        return providerSlots;
    }

    /// <summary>
    /// Gets the body provider slots of this body provider
    /// </summary>
    public List<BodyProviderSlot> GetProviderSlots(Entity<BodyProviderComponent?> ent, BodyProviderType type = BodyProviderType.All, string? id = null)
    {
        if (!_providerQuery.Resolve(ent, ref ent.Comp))
            return new List<BodyProviderSlot>();

        var providerSlots = new List<BodyProviderSlot>();
        foreach (var providerSlot in ent.Comp.Providers.Values)
        {
            if (!type.HasFlag(providerSlot.Type) || !string.IsNullOrEmpty(id) && providerSlot.Id != id)
                continue;

            if (providerSlot.ProviderUid is {} provider && _providerQuery.TryComp(provider, out var providerComp))
                providerSlots.AddRange(GetProviderSlots((provider, providerComp), type, id));

            providerSlots.Add(providerSlot);
        }

        return providerSlots;
    }

    /// <summary>
    /// Gets the body provider slots of this entity.
    /// </summary>
    public List<BodyProviderSlot> GetProviderSlots(EntityUid parent, BodyProviderType type = BodyProviderType.All, string? id = null)
    {
        if (_bodyQuery.TryComp(parent, out var bodyComp))
            return GetProviderSlots((parent, bodyComp), type, id);

        if (_providerQuery.TryComp(parent, out var providerComp))
            return GetProviderSlots((parent, providerComp), type, id);

        return new List<BodyProviderSlot>();
    }

    #endregion

    #endregion
}
