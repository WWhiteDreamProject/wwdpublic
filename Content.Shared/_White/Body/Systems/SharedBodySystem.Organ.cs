using System.Linq;
using Content.Shared._White.Body.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.Containers;

namespace Content.Shared._White.Body.Systems;

public abstract partial class SharedBodySystem
{
    private void InitializeOrgan()
    {
        SubscribeLocalEvent<OrganComponent, EntGotInsertedIntoContainerMessage>(OnOrganGotInserted);
        SubscribeLocalEvent<OrganComponent, EntGotRemovedFromContainerMessage>(OnOrganGotRemoved);
        SubscribeLocalEvent<OrganComponent, OrganRelayedEvent<RejuvenateEvent>>(OnOrganRejuvenate);
    }

    #region Event Handling

    private void OnOrganGotInserted(Entity<OrganComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        var containerSlotId = args.Container.ID;
        if (containerSlotId.IndexOf(OrganSlotContainerIdPrefix, StringComparison.Ordinal) == -1)
            return;

        var parent = args.Container.Owner;
        Entity<BodyComponent> body;

        if (TryComp<BodyComponent>(parent, out var bodyComponent))
            body = (parent, bodyComponent);
        else if (TryComp<BodyPartComponent>(parent, out var bodyPartComponent))
        {
            if (!bodyPartComponent.Body.HasValue || !Resolve(bodyPartComponent.Body.Value, ref bodyComponent))
            {
                ent.Comp.Parent = parent;
                Dirty(ent);

                RaiseLocalEvent(ent, new OrganAddedEvent(ent, null, parent, args.Container.ID));

                return;
            }

            body = (bodyPartComponent.Body.Value, bodyComponent);
        }
        else if (TryComp<BoneComponent>(parent, out var boneComponent))
        {
            if (!boneComponent.Body.HasValue || !Resolve(boneComponent.Body.Value, ref bodyComponent))
            {
                ent.Comp.Parent = parent;
                Dirty(ent);

                RaiseLocalEvent(ent, new OrganAddedEvent(ent, null, parent, containerSlotId));

                return;
            }

            body = (boneComponent.Body.Value, bodyComponent);
        }
        else
            return;

        ent.Comp.Body = body;
        ent.Comp.Parent = parent;
        Dirty(ent);

        SetOrganEnable(ent.AsNullable(), true);

        var ev = new OrganAddedEvent(
            ent,
            body,
            parent,
            args.Container.ID);

        RaiseLocalEvent(ent, ev);
        RaiseLocalEvent(body, ev);
    }

    private void OnOrganGotRemoved(Entity<OrganComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        var containerSlotId = args.Container.ID;
        if (containerSlotId.IndexOf(OrganSlotContainerIdPrefix, StringComparison.Ordinal) == -1)
            return;

        var parent = args.Container.Owner;
        Entity<BodyComponent> body;

        if (TryComp<BodyComponent>(parent, out var bodyComponent))
            body = (parent, bodyComponent);
        else if (TryComp<BodyPartComponent>(parent, out var bodyPartComponent))
        {
            if (!bodyPartComponent.Body.HasValue || !Resolve(bodyPartComponent.Body.Value, ref bodyComponent))
            {
                ent.Comp.Parent = null;
                Dirty(ent);

                RaiseLocalEvent(ent, new OrganRemovedEvent(ent, null, parent, containerSlotId));

                return;
            }

            body = (bodyPartComponent.Body.Value, bodyComponent);
        }
        else if (TryComp<BoneComponent>(parent, out var boneComponent))
        {
            if (!boneComponent.Body.HasValue || !Resolve(boneComponent.Body.Value, ref bodyComponent))
            {
                ent.Comp.Parent = null;
                Dirty(ent);

                RaiseLocalEvent(ent, new OrganRemovedEvent(ent, null, parent, args.Container.ID));

                return;
            }

            body = (boneComponent.Body.Value, bodyComponent);
        }
        else
            return;

        ent.Comp.Body = null;
        ent.Comp.Parent = null;
        Dirty(ent);

        SetOrganEnable(ent.AsNullable(), false);

        var ev = new OrganRemovedEvent(
            ent,
            body,
            parent,
            args.Container.ID);

        RaiseLocalEvent(ent, ev);
        RaiseLocalEvent(body, ev);
    }

    private void OnOrganRejuvenate(Entity<OrganComponent> ent, ref OrganRelayedEvent<RejuvenateEvent> args) =>
        SetOrganEnable(ent.AsNullable(), true);

    #endregion

    #region Private API

    private void SetupOrgans(EntityUid parentUid, Dictionary<string, OrganSlot> organSlots)
    {
        foreach (var (organId, organSlot) in organSlots)
        {
            if (organSlot.HasOrgan || string.IsNullOrEmpty(organSlot.StartingOrgan))
                continue;

            organSlot.ContainerSlot = _container.EnsureContainer<ContainerSlot>(parentUid, GetOrganContainerId(organId));

            var organ = Spawn(organSlot.StartingOrgan);

            if (_container.Insert(organ, organSlot.ContainerSlot))
                continue;

            _sawmill.Error($"Couldn't insert {ToPrettyString(organ)} to {ToPrettyString(parentUid)}");
            QueueDel(organ);
        }
    }

    private void SetOrgansBody(Entity<BodyComponent>? body, EntityUid parent, IEnumerable<Entity<OrganComponent>> organs)
    {
        foreach (var organ in organs)
        {
            if (!_container.TryGetContainingContainer((organ, null, null), out var container) || container.Owner != parent)
                continue;

            if (body.HasValue)
            {
                var ev = new OrganAddedEvent(organ, body, parent, container.ID);
                RaiseLocalEvent(organ, ev);
                RaiseLocalEvent(body.Value, ev);
            }
            else if (TryComp<BodyComponent>(organ.Comp.Body, out var bodyComponent))
            {
                var ev = new OrganRemovedEvent(organ, (organ.Comp.Body.Value, bodyComponent), parent, container.ID);
                RaiseLocalEvent(organ, ev);
                RaiseLocalEvent(organ.Comp.Body.Value, ev);
            }

            organ.Comp.Body = body;
            Dirty(organ);
        }
    }

    #endregion

    #region Public API

    #region GetOrgans

    /// <summary>
    /// Gets the organs of this body.
    /// </summary>
    public List<Entity<OrganComponent>> GetOrgans(Entity<BodyComponent?> body, OrganType type = OrganType.None)
    {
        if (!Resolve(body, ref body.Comp))
            return new List<Entity<OrganComponent>>();

        var organs = new List<Entity<OrganComponent>>();
        foreach (var bodyPartSlot in body.Comp.BodyParts.Values)
        {
            if (!TryComp<BodyPartComponent>(bodyPartSlot.BodyPartUid, out var bodyPartComponent))
                continue;

            organs.AddRange(GetOrgans((bodyPartSlot.BodyPartUid.Value, bodyPartComponent), type));
        }

        foreach (var organSlot in body.Comp.Organs.Values)
        {
            if (!TryComp<OrganComponent>(organSlot.OrganUid, out var organComponent) || !organComponent.Type.HasFlag(type))
                continue;

            organs.Add((organSlot.OrganUid.Value, organComponent));
        }

        return organs;
    }

    /// <summary>
    /// Gets the organs of this body part.
    /// </summary>
    public List<Entity<OrganComponent>> GetOrgans(Entity<BodyPartComponent?> bodyPart, OrganType type = OrganType.None)
    {
        if (!Resolve(bodyPart, ref bodyPart.Comp))
            return new List<Entity<OrganComponent>>();

        var organs = new List<Entity<OrganComponent>>();
        foreach (var organSlot in bodyPart.Comp.Organs.Values)
        {
            if (!TryComp<OrganComponent>(organSlot.OrganUid, out var organComponent) || !organComponent.Type.HasFlag(type))
                continue;

            organs.Add((organSlot.OrganUid.Value, organComponent));
        }

        foreach (var boneSlot in bodyPart.Comp.Bones.Values)
        {
            if (!TryComp<BoneComponent>(boneSlot.BoneUid, out var bone))
                continue;

            organs.AddRange(GetOrgans((boneSlot.BoneUid.Value, bone), type));
        }

        return organs;
    }

    /// <summary>
    /// Gets the organs of this bone.
    /// </summary>
    public List<Entity<OrganComponent>> GetOrgans(Entity<BoneComponent?> bone, OrganType type = OrganType.None)
    {
        if (!Resolve(bone, ref bone.Comp))
            return new List<Entity<OrganComponent>>();

        var organs = new List<Entity<OrganComponent>>();
        foreach (var organSlot in bone.Comp.Organs.Values)
        {
            if (!TryComp<OrganComponent>(organSlot.OrganUid, out var organComponent)
                || !organComponent.Type.HasFlag(type))
                continue;

            organs.Add((organSlot.OrganUid.Value, organComponent));
        }

        return organs;
    }

    /// <summary>
    /// Gets the organs of this entity.
    /// </summary>
    public List<Entity<OrganComponent>> GetOrgans(EntityUid parent, OrganType type = OrganType.None)
    {
        if (TryComp<BodyComponent>(parent, out var bodyComponent))
            return GetOrgans((parent, bodyComponent), type);

        if (TryComp<BodyPartComponent>(parent, out var bodyPartComponent))
            return GetOrgans((parent, bodyPartComponent), type);

        if (TryComp<BoneComponent>(parent, out var boneComponent))
            return GetOrgans((parent, boneComponent), type);

        return new List<Entity<OrganComponent>>();
    }

    #endregion

    #region GetOrgans<T>

    /// <summary>
    /// Gets the organs of this body with the given component.
    /// </summary>
    public List<Entity<OrganComponent, T>> GetOrgans<T>(Entity<BodyComponent?> body, OrganType type = OrganType.None) where T : IComponent
    {
        var organs = new List<Entity<OrganComponent, T>>();
        foreach (var organ in GetOrgans(body, type))
        {
            if (!TryComp<T>(organ, out var component))
                continue;

            organs.Add((organ.Owner, organ.Comp, component));
        }

        return organs;
    }

    /// <summary>
    /// Gets the organs of this body part with the given component.
    /// </summary>
    public List<Entity<OrganComponent, T>> GetOrgans<T>(Entity<BodyPartComponent?> bodyPart, OrganType type = OrganType.None) where T : IComponent
    {
        var organs = new List<Entity<OrganComponent, T>>();
        foreach (var organ in GetOrgans(bodyPart, type))
        {
            if (!TryComp<T>(organ, out var component))
                continue;

            organs.Add((organ.Owner, organ.Comp, component));
        }

        return organs;
    }

    /// <summary>
    /// Gets the organs of this bone with the given component.
    /// </summary>
    public List<Entity<OrganComponent, T>> GetOrgans<T>(Entity<BoneComponent?> bone, OrganType type = OrganType.None) where T : IComponent
    {
        var organs = new List<Entity<OrganComponent, T>>();
        foreach (var organ in GetOrgans(bone, type))
        {
            if (!TryComp<T>(organ, out var component))
                continue;

            organs.Add((organ.Owner, organ.Comp, component));
        }

        return organs;
    }

    /// <summary>
    /// Gets the organs of this entity with the given component.
    /// </summary>
    public List<Entity<OrganComponent, T>> GetOrgans<T>(EntityUid parent, OrganType type = OrganType.None) where T : IComponent
    {
        var organs = new List<Entity<OrganComponent, T>>();
        foreach (var organ in GetOrgans(parent, type))
        {
            if (!TryComp<T>(organ, out var component))
                continue;

            organs.Add((organ.Owner, organ.Comp, component));
        }

        return organs;
    }

    #endregion

    #region GetOrganSlots

    /// <summary>
    /// Gets the organ slots of this body.
    /// </summary>
    public List<OrganSlot> GetOrganSlots(Entity<BodyComponent?> body, OrganType type = OrganType.None, string? slotId = null)
    {
        if (!Resolve(body, ref body.Comp))
            return new List<OrganSlot>();

        var organSlots = new List<OrganSlot>();
        foreach (var bodyPart in GetBodyParts(body))
            organSlots.AddRange(GetOrganSlots(bodyPart.AsNullable(), type, slotId));

        foreach (var organSlot in body.Comp.Organs.Values)
        {
            if (!organSlot.Type.HasFlag(type) || !string.IsNullOrEmpty(slotId) && organSlot.Id != slotId)
                continue;

            organSlots.Add(organSlot);
        }

        return organSlots;
    }

    /// <summary>
    /// Gets the organ slots of this body part.
    /// </summary>
    public List<OrganSlot> GetOrganSlots(Entity<BodyPartComponent?> bodyPart, OrganType type = OrganType.None, string? slotId = null)
    {
        if (!Resolve(bodyPart, ref bodyPart.Comp))
            return new List<OrganSlot>();

        var organSlots = new List<OrganSlot>();
        foreach (var bone in GetBones(bodyPart))
            organSlots.AddRange(GetOrganSlots(bone.AsNullable(), type, slotId));

        foreach (var organSlot in bodyPart.Comp.Organs.Values)
        {
            if (!organSlot.Type.HasFlag(type) || !string.IsNullOrEmpty(slotId) && organSlot.Id != slotId)
                continue;

            organSlots.Add(organSlot);
        }

        return organSlots;
    }

    /// <summary>
    /// Gets the organ slots of this bone.
    /// </summary>
    public List<OrganSlot> GetOrganSlots(Entity<BoneComponent?> bone, OrganType type = OrganType.None, string? slotId = null)
    {
        if (!Resolve(bone, ref bone.Comp))
            return new List<OrganSlot>();

        var organSlots = new List<OrganSlot>();
        foreach (var organSlot in bone.Comp.Organs.Values)
        {
            if (!organSlot.Type.HasFlag(type) || !string.IsNullOrEmpty(slotId) && organSlot.Id != slotId)
                continue;

            organSlots.Add(organSlot);
        }

        return organSlots;
    }

    /// <summary>
    /// Gets the organ slots of this entity.
    /// </summary>
    public List<OrganSlot> GetOrganSlots(EntityUid parent, OrganType type = OrganType.None, string? slotId = null)
    {
        if (TryComp<BodyComponent>(parent, out var bodyComponent))
            return GetOrganSlots((parent, bodyComponent), type, slotId);

        if (TryComp<BodyPartComponent>(parent, out var bodyPartComponent))
            return GetOrganSlots((parent, bodyPartComponent), type, slotId);

        return new List<OrganSlot>();
    }

    #endregion

    #region TryGetOrgans

    /// <summary>
    /// Returns the organs of this body.
    /// </summary>
    public bool TryGetOrgans(Entity<BodyComponent?> body, out List<Entity<OrganComponent>> organs, OrganType type = OrganType.None)
    {
        organs = GetOrgans(body, type);
        return organs.Count != 0;
    }

    /// <summary>
    /// Returns the organs of this body part.
    /// </summary>
    public bool TryGetOrgans(Entity<BodyPartComponent?> bodyPart, out List<Entity<OrganComponent>> organs, OrganType type = OrganType.None)
    {
        organs = GetOrgans(bodyPart, type);
        return organs.Count != 0;
    }

    /// <summary>
    /// Returns the organs of this bone.
    /// </summary>
    public bool TryGetOrgans(Entity<BoneComponent?> bone, out List<Entity<OrganComponent>> organs, OrganType type = OrganType.None)
    {
        organs = GetOrgans(bone, type);
        return organs.Count != 0;
    }

    /// <summary>
    /// Returns the organs of this entity.
    /// </summary>
    public bool TryGetOrgans(EntityUid parent, out List<Entity<OrganComponent>> organs, OrganType type = OrganType.None)
    {
        organs = GetOrgans(parent, type);
        return organs.Count != 0;
    }

    #endregion

    #region TryGetOrgans<T>

    /// <summary>
    /// Returns the organs of this body with the given component.
    /// </summary>
    public bool TryGetOrgans<T>(Entity<BodyComponent?> body, out List<Entity<OrganComponent, T>> organs, OrganType type = OrganType.None) where T : IComponent
    {
        organs = GetOrgans<T>(body, type);
        return organs.Count != 0;
    }

    /// <summary>
    /// Returns the organs of this body part with the given component.
    /// </summary>
    public bool TryGetOrgans<T>(Entity<BodyPartComponent?> bodyPart, out List<Entity<OrganComponent, T>> organs, OrganType type = OrganType.None) where T : IComponent
    {
        organs = GetOrgans<T>(bodyPart, type);
        return organs.Count != 0;
    }

    /// <summary>
    /// Returns the organs of this bone with the given component.
    /// </summary>
    public bool TryGetOrgans<T>(Entity<BoneComponent?> bone, out List<Entity<OrganComponent, T>> organs, OrganType type = OrganType.None) where T : IComponent
    {
        organs = GetOrgans<T>(bone, type);
        return organs.Count != 0;
    }

    /// <summary>
    /// Returns the organs of this entity with the given component.
    /// </summary>
    public bool TryGetOrgans<T>(EntityUid parent, out List<Entity<OrganComponent, T>> organs, OrganType type = OrganType.None) where T : IComponent
    {
        organs = GetOrgans<T>(parent, type);
        return organs.Count != 0;
    }

    #endregion

    #region TryCreateOrganSlot

    /// <summary>
    /// Trying to create an organ slot for this body.
    /// </summary>
    public bool TryCreateOrganSlot(Entity<BodyComponent?> body, string organId, OrganType type)
    {
        if (!Resolve(body, ref body.Comp))
            return false;

        if (body.Comp.Organs.ContainsKey(organId))
            return true;

        var organSlot = new OrganSlot
        {
            Type = type,
            ContainerSlot = _container.EnsureContainer<ContainerSlot>(body, GetOrganContainerId(organId))
        };

        if (!body.Comp.Organs.TryAdd(organId, organSlot))
            return false;

        Dirty(body);
        return true;
    }

    /// <summary>
    /// Trying to create an organ slot for this body part.
    /// </summary>
    public bool TryCreateOrganSlot(Entity<BodyPartComponent?> bodyPart, string organId, OrganType type)
    {
        if (!Resolve(bodyPart, ref bodyPart.Comp))
            return false;

        if (bodyPart.Comp.Organs.ContainsKey(organId))
            return true;

        var organSlot = new OrganSlot
        {
            Type = type,
            ContainerSlot = _container.EnsureContainer<ContainerSlot>(bodyPart, GetOrganContainerId(organId))
        };

        if (!bodyPart.Comp.Organs.TryAdd(organId, organSlot))
            return false;

        Dirty(bodyPart);
        return true;
    }

    /// <summary>
    /// Trying to create an organ slot for this bone.
    /// </summary>
    public bool TryCreateOrganSlot(Entity<BoneComponent?> bone, string organId, OrganType type)
    {
        if (!Resolve(bone, ref bone.Comp))
            return false;

        if (bone.Comp.Organs.ContainsKey(organId))
            return true;

        var organSlot = new OrganSlot
        {
            Type = type,
            ContainerSlot = _container.EnsureContainer<ContainerSlot>(bone, GetOrganContainerId(organId))
        };

        if (!bone.Comp.Organs.TryAdd(organId, organSlot))
            return false;

        Dirty(bone);
        return true;
    }

    /// <summary>
    /// Trying to create an organ slot for this entity.
    /// </summary>
    public bool TryCreateOrganSlot(EntityUid parent, string organId, OrganType type)
    {
        if (TryComp<BodyComponent>(parent, out var bodyComponent))
            return TryCreateOrganSlot((parent, bodyComponent), organId, type);

        if (TryComp<BodyPartComponent>(parent, out var bodyPartComponent))
            return TryCreateOrganSlot((parent, bodyPartComponent), organId, type);

        if (TryComp<BoneComponent>(parent, out var boneComponent))
            return TryCreateOrganSlot((parent, boneComponent), organId, type);

        return false;
    }

    #endregion

    #region TryAttachOrgan

    /// <summary>
    /// Trying to attach an organ to this body.
    /// </summary>
    public bool TryAttachOrgan(Entity<BodyComponent?> body, Entity<OrganComponent?> organ, string slotId)
    {
        if (!Resolve(organ, ref organ.Comp)
            || GetOrganSlots(body, organ.Comp.Type, slotId).FirstOrDefault() is not {} organSlot
            || organSlot.ContainerSlot is null)
            return false;

        return _container.Insert(organ.Owner, organSlot.ContainerSlot);
    }

    /// <summary>
    /// Trying to attach an organ to this body part.
    /// </summary>
    public bool TryAttachOrgan(Entity<BodyPartComponent?> bodyPart, Entity<OrganComponent?> organ, string slotId)
    {
        if (!Resolve(organ, ref organ.Comp)
            || GetOrganSlots(bodyPart, organ.Comp.Type, slotId).FirstOrDefault() is not {} organSlot
            || organSlot.ContainerSlot is null)
            return false;

        return _container.Insert(organ.Owner, organSlot.ContainerSlot);
    }

    /// <summary>
    /// Trying to attach an organ to this bone.
    /// </summary>
    public bool TryAttachOrgan(Entity<BoneComponent?> bone, Entity<OrganComponent?> organ, string slotId)
    {
        if (!Resolve(organ, ref organ.Comp)
            || GetOrganSlots(bone, organ.Comp.Type, slotId).FirstOrDefault() is not {} organSlot
            || organSlot.ContainerSlot is null)
            return false;

        return _container.Insert(organ.Owner, organSlot.ContainerSlot);
    }

    /// <summary>
    /// Trying to attach an organ to this entity.
    /// </summary>
    public bool TryAttachOrgan(EntityUid parent, Entity<OrganComponent?> organ, string slotId)
    {
        if (!Resolve(organ, ref organ.Comp)
            || GetOrganSlots(parent, organ.Comp.Type, slotId).FirstOrDefault() is not {} organSlot
            || organSlot.ContainerSlot is null)
            return false;

        return _container.Insert(organ.Owner, organSlot.ContainerSlot);
    }

    #endregion

    #region TryCreateOrganSlotAndAttachOrgan

    /// <summary>
    /// Trying to create an organ slot for this body and attach an organ to this body.
    /// </summary>
    public bool TryCreateOrganSlotAndAttachOrgan(
        Entity<BodyComponent?> body,
        Entity<OrganComponent?> organ,
        string slotId,
        OrganType type
        ) =>
        TryCreateOrganSlot(body, slotId, type) && TryAttachOrgan(body, organ, slotId);

    /// <summary>
    /// Trying to create an organ slot for this body part and attach an organ to this body part.
    /// </summary>
    public bool TryCreateOrganSlotAndAttachOrgan(
        Entity<BodyPartComponent?> bodyPart,
        Entity<OrganComponent?> organ,
        string slotId,
        OrganType type
        ) =>
        TryCreateOrganSlot(bodyPart, slotId, type) && TryAttachOrgan(bodyPart, organ, slotId);

    /// <summary>
    /// Trying to create an organ slot for this bone and attach an organ to this bone.
    /// </summary>
    public bool TryCreateOrganSlotAndAttachOrgan(
        Entity<BoneComponent?> bone,
        Entity<OrganComponent?> organ,
        string slotId,
        OrganType type
        ) =>
        TryCreateOrganSlot(bone, slotId, type) && TryAttachOrgan(bone, organ, slotId);

    /// <summary>
    /// Trying to create an organ slot for this entity and attach an organ to this entity.
    /// </summary>
    public bool TryCreateOrganSlotAndAttachOrgan(
        EntityUid parent,
        Entity<OrganComponent?> organ,
        string slotId,
        OrganType type
        ) =>
        TryCreateOrganSlot(parent, slotId, type) && TryAttachOrgan(parent, organ, slotId);

    #endregion

    /// <summary>
    /// Toggles the organ's functionality.
    /// </summary>
    public void SetOrganEnable(Entity<OrganComponent?> organ, bool value)
    {
        if (!Resolve(organ, ref organ.Comp) || organ.Comp.Enable == value)
            return;

        organ.Comp.Enable = value;
        RaiseLocalEvent(organ, new AfterOrganToggledEvent(value));
    }

    #endregion
}
