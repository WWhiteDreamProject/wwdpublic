using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._White.Body.Components;
using Robust.Shared.Containers;

namespace Content.Shared._White.Body.Systems;

public abstract partial class SharedBodySystem
{
    private void InitializeBodyPart()
    {
        SubscribeLocalEvent<BodyPartComponent, MapInitEvent>(OnBodyPartMapInit);

        SubscribeLocalEvent<BodyPartComponent, EntGotInsertedIntoContainerMessage>(OnBodyPartGotInserted);
        SubscribeLocalEvent<BodyPartComponent, EntGotRemovedFromContainerMessage>(OnBodyPartGotRemoved);
    }

    #region Event Handling

    private void OnBodyPartMapInit(Entity<BodyPartComponent> bodyPart, ref MapInitEvent args)
    {
        SetupBodyParts(bodyPart, bodyPart.Comp.BodyParts);

        SetupBones(bodyPart, bodyPart.Comp.Bones);

        SetupOrgans(bodyPart, bodyPart.Comp.Organs);
    }

    private void OnBodyPartGotInserted(Entity<BodyPartComponent> bodyPart, ref EntGotInsertedIntoContainerMessage args)
    {
        var containerSlotId = args.Container.ID;
        if (containerSlotId.IndexOf(BodyPartSlotContainerIdPrefix, StringComparison.Ordinal) == -1)
            return;

        var parent = args.Container.Owner;
        Entity<BodyComponent> body;

        if (TryComp<BodyComponent>(parent, out var bodyComponent))
            body = (parent, bodyComponent);
        else if (TryComp<BodyPartComponent>(parent, out var parentBodyPartComponent))
        {
            if (!parentBodyPartComponent.Body.HasValue || !Resolve(parentBodyPartComponent.Body.Value, ref bodyComponent))
            {
                bodyPart.Comp.Parent = parent;
                Dirty(bodyPart);

                RaiseLocalEvent(bodyPart, new BodyPartAddedEvent(bodyPart, null, parent, containerSlotId));

                return;
            }

            body = (parentBodyPartComponent.Body.Value, bodyComponent);
        }
        else
            return;

        SetBodyPartsBody(body, bodyPart, GetBodyParts(bodyPart.AsNullable()));
        SetBonesBody(body, bodyPart, GetBones(bodyPart.AsNullable()));
        SetOrgansBody(body, bodyPart, GetOrgans(bodyPart.AsNullable()));

        foreach (var bodyPartSlot in GetBodyPartSlots(bodyPart.AsNullable()))
        {
            if (string.IsNullOrEmpty(bodyPartSlot.Id))
                continue;

            var slotId = GetBodyPartSlotId(bodyPartSlot.Id);

            if (!body.Comp.BodyParts.TryAdd(slotId, bodyPartSlot))
                body.Comp.BodyParts[slotId] = bodyPartSlot;
        }

        bodyPart.Comp.Body = body;
        bodyPart.Comp.Parent = parent;
        Dirty(bodyPart);

        var ev = new BodyPartAddedEvent(
            bodyPart,
            body,
            parent,
            args.Container.ID);

        RaiseLocalEvent(bodyPart, ev);
        RaiseLocalEvent(body, ev);
    }

    private void OnBodyPartGotRemoved(Entity<BodyPartComponent> bodyPart, ref EntGotRemovedFromContainerMessage args)
    {
        var containerSlotId = args.Container.ID;
        if (containerSlotId.IndexOf(BodyPartSlotContainerIdPrefix, StringComparison.Ordinal) == -1)
            return;

        var parent = args.Container.Owner;
        Entity<BodyComponent> body;

        if (TryComp<BodyComponent>(parent, out var bodyComponent))
            body = (parent, bodyComponent);
        else if (TryComp<BodyPartComponent>(parent, out var parentBodyPartComponent))
        {
            if (!parentBodyPartComponent.Body.HasValue || !Resolve(parentBodyPartComponent.Body.Value, ref bodyComponent))
            {
                bodyPart.Comp.Parent = null;
                Dirty(bodyPart);

                RaiseLocalEvent(bodyPart, new BodyPartRemovedEvent(bodyPart, null, parent, containerSlotId));

                return;
            }

            body = (parentBodyPartComponent.Body.Value, bodyComponent);
        }
        else
            return;

        SetBodyPartsBody(null, bodyPart, GetBodyParts(bodyPart.AsNullable()));
        SetBonesBody(null, bodyPart, GetBones(bodyPart.AsNullable()));
        SetOrgansBody(null, bodyPart, GetOrgans(bodyPart.AsNullable()));

        foreach (var bodyPartSlot in GetBodyPartSlots(bodyPart.AsNullable()))
        {
            if (string.IsNullOrEmpty(bodyPartSlot.Id))
                continue;

            body.Comp.BodyParts.Remove(GetBodyPartSlotId(bodyPartSlot.Id));
        }

        bodyPart.Comp.Body = null;
        bodyPart.Comp.Parent = null;
        Dirty(bodyPart);

        var ev = new BodyPartRemovedEvent(
            bodyPart,
            body,
            parent,
            args.Container.ID);

        RaiseLocalEvent(bodyPart, ev);
        RaiseLocalEvent(body, ev);
    }

    #endregion

    #region Private API

    private void SetupBodyParts(EntityUid parentUid, Dictionary<string, BodyPartSlot> bodyPartSlots)
    {
        foreach (var (bodyPartId, bodyPartSlot) in bodyPartSlots)
        {
            if (bodyPartSlot.HasBodyPart || string.IsNullOrEmpty(bodyPartSlot.StartingBodyPart))
                return;

            bodyPartSlot.ContainerSlot = _container.EnsureContainer<ContainerSlot>(parentUid, GetBodyPartSlotContainerId(bodyPartId));

            var bodyPart = Spawn(bodyPartSlot.StartingBodyPart);
            if (!TryComp<BodyPartComponent>(bodyPart, out var bodyPartComponent))
            {
                _sawmill.Error($"Body part {ToPrettyString(bodyPart)} does not have {typeof(BodyPartComponent)} {bodyPartSlot.StartingBodyPart}");
                QueueDel(bodyPart);
                return;
            }

            if (!_container.Insert(bodyPart, bodyPartSlot.ContainerSlot))
            {
                _sawmill.Error($"Couldn't insert {ToPrettyString(bodyPart)} to {ToPrettyString(parentUid)}");
                QueueDel(bodyPart);
                return;
            }

            bodyPartComponent.Bones = bodyPartSlot.Bones;
            SetupBones(bodyPart, bodyPartComponent.Bones);

            bodyPartComponent.Organs = bodyPartSlot.Organs;
            SetupOrgans(bodyPart, bodyPartComponent.Organs);
        }
    }

    private void SetBodyPartsBody(Entity<BodyComponent>? body, EntityUid parent, List<Entity<BodyPartComponent>> bodyParts)
    {
        foreach (var bodyPart in bodyParts)
        {
            if (!_container.TryGetContainingContainer((bodyPart, null, null), out var container) || container.Owner != parent)
                continue;

            if (body.HasValue)
            {
                var ev = new BodyPartAddedEvent(bodyPart, body, parent, container.ID);
                RaiseLocalEvent(bodyPart, ev);
                RaiseLocalEvent(body.Value, ev);
            }
            else if (TryComp<BodyComponent>(bodyPart.Comp.Body, out var bodyComponent))
            {
                var ev = new BodyPartRemovedEvent(bodyPart, (bodyPart.Comp.Body.Value, bodyComponent), parent, container.ID);
                RaiseLocalEvent(bodyPart, ev);
                RaiseLocalEvent(bodyPart.Comp.Body.Value, ev);
            }

            bodyPart.Comp.Body = body;
            Dirty(bodyPart);

            SetBodyPartsBody(body, bodyPart, GetBodyParts(bodyPart.AsNullable()));
            SetBonesBody(body, bodyPart, GetBones(bodyPart.AsNullable()));
            SetOrgansBody(body, bodyPart, GetOrgans(bodyPart.AsNullable()));
        }
    }

    #endregion

    #region Public API

    #region GetBodyParts

    /// <summary>
    /// Gets the body parts of this body.
    /// </summary>
    public List<Entity<BodyPartComponent>> GetBodyParts(Entity<BodyComponent?> body, BodyPartType type = BodyPartType.All)
    {
        if (!Resolve(body, ref body.Comp))
            return new List<Entity<BodyPartComponent>>();

        var bodyParts = new List<Entity<BodyPartComponent>>();
        foreach (var bodyPartSlot in body.Comp.BodyParts.Values)
        {
            if (!TryComp<BodyPartComponent>(bodyPartSlot.BodyPartUid, out var bodyPartComponent)
                || !type.HasFlag(bodyPartSlot.Type))
                continue;

            bodyParts.Add((bodyPartSlot.BodyPartUid.Value, bodyPartComponent));
        }

        return bodyParts;
    }

    /// <summary>
    /// Gets the body parts of this body part.
    /// </summary>
    public List<Entity<BodyPartComponent>> GetBodyParts(Entity<BodyPartComponent?> bodyPart, BodyPartType type = BodyPartType.All)
    {
        if (!Resolve(bodyPart, ref bodyPart.Comp))
            return new List<Entity<BodyPartComponent>>();

        var bodyParts = new List<Entity<BodyPartComponent>>();
        foreach (var bodyPartSlot in bodyPart.Comp.BodyParts.Values)
        {
            if (!TryComp<BodyPartComponent>(bodyPartSlot.BodyPartUid, out var bodyPartComponent)
                || !type.HasFlag(bodyPartSlot.Type))
                continue;

            bodyParts.Add((bodyPartSlot.BodyPartUid.Value, bodyPartComponent));
            bodyParts.AddRange(GetBodyParts((bodyPartSlot.BodyPartUid.Value, bodyPartComponent), type));
        }

        return bodyParts;
    }

    /// <summary>
    /// Gets the body parts of this entity.
    /// </summary>
    public List<Entity<BodyPartComponent>> GetBodyParts(EntityUid parent, BodyPartType type = BodyPartType.All)
    {
        if (TryComp<BodyComponent>(parent, out var bodyComponent))
            return GetBodyParts((parent, bodyComponent), type);

        if (TryComp<BodyPartComponent>(parent, out var bodyPartComponent))
            return GetBodyParts((parent, bodyPartComponent), type);

        return new List<Entity<BodyPartComponent>>();
    }

    #endregion

    #region GetBodyParts<T>

    /// <summary>
    /// Gets the body parts of this body with the given component.
    /// </summary>
    public List<Entity<BodyPartComponent, T>> GetBodyParts<T>(Entity<BodyComponent?> body, BodyPartType type = BodyPartType.All) where T : IComponent
    {
        var bodyParts = new List<Entity<BodyPartComponent, T>>();
        foreach (var bodyPart in GetBodyParts(body, type))
        {
            if (!TryComp<T>(bodyPart, out var component))
                continue;

            bodyParts.Add((bodyPart.Owner, bodyPart.Comp, component));
        }

        return bodyParts;
    }

    /// <summary>
    /// Gets the body parts of this body part with the given component.
    /// </summary>
    public List<Entity<BodyPartComponent, T>> GetBodyParts<T>(Entity<BodyPartComponent?> bodyPart, BodyPartType type = BodyPartType.All) where T : IComponent
    {
        var bodyParts = new List<Entity<BodyPartComponent, T>>();
        foreach (var childBodyPart in GetBodyParts(bodyPart, type))
        {
            if (!TryComp<T>(childBodyPart, out var component))
                continue;

            bodyParts.Add((bodyPart.Owner, childBodyPart.Comp, component));
        }

        return bodyParts;
    }

    /// <summary>
    /// Gets the body parts of this body part with the given component.
    /// </summary>
    public List<Entity<BodyPartComponent, T>> GetBodyParts<T>(EntityUid parent, BodyPartType type = BodyPartType.All) where T : IComponent
    {
        var bodyParts = new List<Entity<BodyPartComponent, T>>();
        foreach (var bodyPart in GetBodyParts(parent, type))
        {
            if (!TryComp<T>(bodyPart, out var component))
                continue;

            bodyParts.Add((bodyPart.Owner, bodyPart.Comp, component));
        }

        return bodyParts;
    }

    #endregion

    #region GetBodyPartSlots

    /// <summary>
    /// Gets the body part slots of this body.
    /// </summary>
    public List<BodyPartSlot> GetBodyPartSlots(Entity<BodyComponent?> body, BodyPartType type = BodyPartType.All, string? slotId = null)
    {
        if (!Resolve(body, ref body.Comp))
            return new List<BodyPartSlot>();

        var bodyPartSlots = new List<BodyPartSlot>();
        foreach (var bodyPartSlot in body.Comp.BodyParts.Values)
        {
            if (!type.HasFlag(bodyPartSlot.Type) || !string.IsNullOrEmpty(slotId) && bodyPartSlot.Id != slotId)
                continue;

            bodyPartSlots.Add(bodyPartSlot);
        }

        return bodyPartSlots;
    }

    /// <summary>
    /// Gets the body part slots of this body part.
    /// </summary>
    public List<BodyPartSlot> GetBodyPartSlots(Entity<BodyPartComponent?> bodyPart, BodyPartType type = BodyPartType.All, string? slotId = null)
    {
        if (!Resolve(bodyPart, ref bodyPart.Comp))
            return new List<BodyPartSlot>();

        var bodyPartSlots = new List<BodyPartSlot>();
        foreach (var bodyPartSlot in bodyPart.Comp.BodyParts.Values)
        {
            if (!type.HasFlag(bodyPartSlot.Type) || !string.IsNullOrEmpty(slotId) && bodyPartSlot.Id != slotId)
                continue;

            if (bodyPartSlot.BodyPartUid.HasValue)
                bodyPartSlots.AddRange(GetBodyPartSlots(bodyPartSlot.BodyPartUid.Value, type, slotId));

            bodyPartSlots.Add(bodyPartSlot);
        }

        return bodyPartSlots;
    }

    /// <summary>
    /// Gets the body part slots of this entity.
    /// </summary>
    public List<BodyPartSlot> GetBodyPartSlots(EntityUid parent, BodyPartType type = BodyPartType.All, string? slotId = null)
    {
        if (TryComp<BodyComponent>(parent, out var bodyComponent))
            return GetBodyPartSlots((parent, bodyComponent), type, slotId);

        if (TryComp<BodyPartComponent>(parent, out var bodyPartComponent))
            return GetBodyPartSlots((parent, bodyPartComponent), type, slotId);

        return new List<BodyPartSlot>();
    }

    #endregion

    #region TryGetBodyParts

    /// <summary>
    /// Returns the body parts of this body.
    /// </summary>
    public bool TryGetBodyParts(Entity<BodyComponent?> body, out List<Entity<BodyPartComponent>> bodyParts, BodyPartType type = BodyPartType.All)
    {
        bodyParts = GetBodyParts(body, type);
        return bodyParts.Count != 0;
    }

    /// <summary>
    /// Returns the body parts of this body part.
    /// </summary>
    public bool TryGetBodyParts(Entity<BodyPartComponent?> bodyPart, out List<Entity<BodyPartComponent>> bodyParts, BodyPartType type = BodyPartType.All)
    {
        bodyParts = GetBodyParts(bodyPart, type);
        return bodyParts.Count != 0;
    }

    /// <summary>
    /// Returns the body parts of this entity.
    /// </summary>
    public bool TryGetBodyParts(EntityUid parent, out List<Entity<BodyPartComponent>> bodyParts, BodyPartType type = BodyPartType.All)
    {
        bodyParts = GetBodyParts(parent, type);
        return bodyParts.Count != 0;
    }

    #endregion

    #region TryGetBodyParts<T>

    /// <summary>
    /// Returns the body parts of this body with the given component.
    /// </summary>
    public bool TryGetBodyParts<T>(Entity<BodyComponent?> body, out List<Entity<BodyPartComponent, T>> bodyParts, BodyPartType type = BodyPartType.All) where T : IComponent
    {
        bodyParts = GetBodyParts<T>(body, type);
        return bodyParts.Count != 0;
    }

    /// <summary>
    /// Returns the body parts of this body part with the given component.
    /// </summary>
    public bool TryGetBodyParts<T>(Entity<BodyPartComponent?> bodyPart, out List<Entity<BodyPartComponent, T>> bodyParts, BodyPartType type = BodyPartType.All) where T : IComponent
    {
        bodyParts = GetBodyParts<T>(bodyPart, type);
        return bodyParts.Count != 0;
    }

    /// <summary>
    /// Returns the body parts of this entity with the given component.
    /// </summary>
    public bool TryGetBodyParts<T>(EntityUid parent, out List<Entity<BodyPartComponent, T>> bodyParts, BodyPartType type = BodyPartType.All) where T : IComponent
    {
        bodyParts = GetBodyParts<T>(parent, type);
        return bodyParts.Count != 0;
    }

    #endregion

    #region TryCreateBodyPartSlot

    /// <summary>
    /// Trying to create an body part slot for this body.
    /// </summary>
    public bool TryCreateBodyPartSlot(Entity<BodyComponent?> body, string bodyPartId, BodyPartType type)
    {
        if (!Resolve(body, ref body.Comp))
            return false;

        var bodyPartSlot = new BodyPartSlot
        {
            Type = type,
            ContainerSlot = _container.EnsureContainer<ContainerSlot>(body, GetBodyPartSlotContainerId(bodyPartId))
        };

        if (!body.Comp.BodyParts.TryAdd(bodyPartId, bodyPartSlot))
            return false;

        Dirty(body);
        return true;
    }

    /// <summary>
    /// Trying to create an body part slot for this body part.
    /// </summary>
    public bool TryCreateBodyPartSlot(Entity<BodyPartComponent?> bodyPart, string bodyPartId, BodyPartType type)
    {
        if (!Resolve(bodyPart, ref bodyPart.Comp))
            return false;

        var bodyPartSlot = new BodyPartSlot
        {
            Type = type,
            ContainerSlot = _container.EnsureContainer<ContainerSlot>(bodyPart, GetBodyPartSlotContainerId(bodyPartId))
        };

        if (TryComp<BodyComponent>(bodyPart.Comp.Body, out var bodyComponent) && !bodyComponent.BodyParts.TryAdd(bodyPartId, bodyPartSlot))
            return false;

        if (!bodyPart.Comp.BodyParts.TryAdd(bodyPartId, bodyPartSlot))
        {
            bodyComponent?.BodyParts.Remove(bodyPartId);
            return false;
        }

        Dirty(bodyPart);
        return true;
    }

    /// <summary>
    /// Trying to create an entity slot for this body part.
    /// </summary>
    public bool TryCreateBodyPartSlot(EntityUid parent, string bodyPartId, BodyPartType type)
    {
        if (TryComp<BodyComponent>(parent, out var bodyComponent))
            return TryCreateBodyPartSlot((parent, bodyComponent), bodyPartId, type);

        if (TryComp<BodyPartComponent>(parent, out var bodyPartComponent))
            return TryCreateBodyPartSlot((parent, bodyPartComponent), bodyPartId, type);

        return false;
    }

    #endregion

    #region TryAttachBodyPart

    /// <summary>
    /// Trying to attach a body part to this body.
    /// </summary>
    public bool TryAttachBodyPart(Entity<BodyComponent?> body, Entity<BodyPartComponent?> bodyPart, string? slotId = null)
    {
        if (!Resolve(bodyPart, ref bodyPart.Comp)
            || GetBodyPartSlots(body, bodyPart.Comp.Type, slotId).FirstOrDefault() is not {} bodyPartSlot
            || bodyPartSlot.ContainerSlot is null)
            return false;

        return _container.Insert(bodyPart.Owner, bodyPartSlot.ContainerSlot);
    }

    /// <summary>
    /// Trying to attach a body part to this body part.
    /// </summary>
    public bool TryAttachBodyPart(Entity<BodyPartComponent?> parentBodyPart, Entity<BodyPartComponent?> childBodyPart, string? slotId = null)
    {
        if (!Resolve(childBodyPart, ref childBodyPart.Comp)
            || GetBodyPartSlots(parentBodyPart, childBodyPart.Comp.Type, slotId).FirstOrDefault() is not {} bodyPartSlot
            || bodyPartSlot.ContainerSlot is null)
            return false;

        return _container.Insert(childBodyPart.Owner, bodyPartSlot.ContainerSlot);
    }

    /// <summary>
    /// Trying to attach a body part to this entity.
    /// </summary>
    public bool TryAttachBodyPart(EntityUid parent, Entity<BodyPartComponent?> bodyPart, string? slotId = null)
    {
        if (!Resolve(bodyPart, ref bodyPart.Comp)
            || GetBodyPartSlots(parent, bodyPart.Comp.Type, slotId).FirstOrDefault() is not {} bodyPartSlot
            || bodyPartSlot.ContainerSlot is null)
            return false;

        return _container.Insert(bodyPart.Owner, bodyPartSlot.ContainerSlot);
    }

    #endregion

    #region TryDetachBodyPart

    /// <summary>
    /// Trying to detach a body part.
    /// </summary>
    public bool TryDetachBodyPart(EntityUid bodyPart)
    {
        if (!_container.TryGetContainingContainer((bodyPart, null, null), out var container))
            return false;

        return _container.Remove(bodyPart, container);
    }

    #endregion

    #region TryCreateBodyPartSlotAndAttachBodyPart

    /// <summary>
    /// Trying to create an body part slot for this body and attach an body part to this body.
    /// </summary>
    public bool TryCreateBodyPartSlotAndAttachBodyPart(
        Entity<BodyComponent?> body,
        Entity<BodyPartComponent?> bodyPart,
        string slotId,
        BodyPartType type
    ) =>
        TryCreateBodyPartSlot(body, slotId, type) && TryAttachBodyPart(body, bodyPart, slotId);

    /// <summary>
    /// Trying to create an body part slot for this body part and attach an body part to this body part.
    /// </summary>
    public bool TryCreateBodyPartSlotAndAttachBodyPart(
        Entity<BodyPartComponent?> parentBodyPart,
        Entity<BodyPartComponent?> childBodyPart,
        string slotId,
        BodyPartType type
    ) =>
        TryCreateBodyPartSlot(parentBodyPart, slotId, type) && TryAttachBodyPart(parentBodyPart, childBodyPart, slotId);

    /// <summary>
    /// Trying to create an body part slot for this entity and attach an body part to this entity.
    /// </summary>
    public bool TryCreateBodyPartSlotAndAttachBodyPart(
        EntityUid parent,
        Entity<BodyPartComponent?> bodyPart,
        string slotId,
        BodyPartType type
    ) =>
        TryCreateBodyPartSlot(parent, slotId, type) && TryAttachBodyPart(parent, bodyPart, slotId);

    #endregion

    /// <summary>
    /// Trying to get the root body part of this body.
    /// </summary>
    public bool TryGetRootBodyPart(Entity<BodyComponent?> body, [NotNullWhen(true)] out EntityUid? bodyPart)
    {
        bodyPart = null;
        if (!Resolve(body, ref body.Comp))
            return false;

        var rootBodyPartId = Prototype.Index(body.Comp.Prototype).Root;

        if (body.Comp.BodyParts.TryGetValue(rootBodyPartId, out var bodyPartSlot))
            return false;

        bodyPart = bodyPartSlot?.BodyPartUid;
        return bodyPart.HasValue;
    }

    #endregion
}
