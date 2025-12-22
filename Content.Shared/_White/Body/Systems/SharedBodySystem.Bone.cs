using System.Linq;
using Content.Shared._White.Body.Components;
using Robust.Shared.Containers;

namespace Content.Shared._White.Body.Systems;

public abstract partial class SharedBodySystem
{
    private void InitializeBone()
    {
        SubscribeLocalEvent<BoneComponent, MapInitEvent>(OnBoneMapInit);

        SubscribeLocalEvent<BoneComponent, EntGotInsertedIntoContainerMessage>(OnBoneGotInserted);
        SubscribeLocalEvent<BoneComponent, EntGotRemovedFromContainerMessage>(OnBoneGotRemoved);
    }

    #region Event Handling

    private void OnBoneMapInit(Entity<BoneComponent> bone, ref MapInitEvent args) => SetupOrgans(bone, bone.Comp.Organs);

    private void OnBoneGotInserted(Entity<BoneComponent> bone, ref EntGotInsertedIntoContainerMessage args)
    {
        var containerSlotId = args.Container.ID;
        if (containerSlotId.IndexOf(BoneSlotContainerIdPrefix, StringComparison.Ordinal) == -1)
            return;

        var parent = args.Container.Owner;

        if (!TryComp<BodyPartComponent>(parent, out var bodyPartComponent))
            return;

        bone.Comp.Parent = parent;

        if (!TryComp<BodyComponent>(bodyPartComponent.Body, out var bodyComponent))
        {
            Dirty(bone);

            RaiseLocalEvent(bone, new BoneAddedEvent(bone, null, parent, containerSlotId));

            return;
        }

        Entity<BodyComponent> body = (bodyPartComponent.Body.Value, bodyComponent);

        SetOrgansBody(body, bone, GetOrgans(bone.AsNullable()));

        bone.Comp.Body = body;
        Dirty(bone);

        var ev = new BoneAddedEvent(
            bone,
            body,
            parent,
            args.Container.ID);

        RaiseLocalEvent(bone, ev);
        RaiseLocalEvent(body, ev);
    }

    private void OnBoneGotRemoved(Entity<BoneComponent> bone, ref EntGotRemovedFromContainerMessage args)
    {
        var containerSlotId = args.Container.ID;
        if (containerSlotId.IndexOf(BoneSlotContainerIdPrefix, StringComparison.Ordinal) == -1)
            return;

        var parent = args.Container.Owner;

        if (!TryComp<BodyPartComponent>(parent, out var bodyPartComponent))
            return;

        bone.Comp.Parent = null;

        if (!TryComp<BodyComponent>(bodyPartComponent.Body, out var bodyComponent))
        {
            Dirty(bone);

            RaiseLocalEvent(bone, new BoneAddedEvent(bone, null, parent, containerSlotId));

            return;
        }

        Entity<BodyComponent> body = (bodyPartComponent.Body.Value, bodyComponent);

        SetOrgansBody(null, bone, GetOrgans(bone.AsNullable()));

        bone.Comp.Body = null;
        Dirty(bone);

        var ev = new BoneRemovedEvent(
            bone,
            body,
            parent,
            args.Container.ID);

        RaiseLocalEvent(bone, ev);
        RaiseLocalEvent(body, ev);
    }

    #endregion

    #region Private API

    private void SetupBones(EntityUid parentUid, Dictionary<string, BoneSlot> boneSlots)
    {
        foreach (var (boneId, boneSlot) in boneSlots)
        {
            if (boneSlot.HasBone || string.IsNullOrEmpty(boneSlot.StartingBone))
                continue;

            boneSlot.ContainerSlot = _container.EnsureContainer<ContainerSlot>(parentUid, GetBoneContainerId(boneId));

            var bone = Spawn(boneSlot.StartingBone);
            if (!TryComp<BoneComponent>(bone, out var boneComponent))
            {
                _sawmill.Error($"Bone {ToPrettyString(bone)} does not have {typeof(BoneComponent)}");
                QueueDel(bone);
                continue;
            }

            if (!_container.Insert(bone, boneSlot.ContainerSlot))
            {
                _sawmill.Error($"Couldn't insert {ToPrettyString(bone)} to {ToPrettyString(parentUid)}");
                QueueDel(bone);
                continue;
            }

            boneComponent.Organs = boneSlot.Organs;
            SetupOrgans(bone, boneComponent.Organs);
        }
    }

    private void SetBonesBody(Entity<BodyComponent>? body, EntityUid parent, List<Entity<BoneComponent>> bones)
    {
        foreach (var bone in bones)
        {
            if (!_container.TryGetContainingContainer((bone, null, null), out var container) || container.Owner != parent)
                continue;

            if (body.HasValue)
            {
                var ev = new BoneAddedEvent(bone, body, parent, container.ID);
                RaiseLocalEvent(bone, ev);
                RaiseLocalEvent(body.Value, ev);
            }
            else if (TryComp<BodyComponent>(bone.Comp.Body, out var bodyComponent))
            {
                var ev = new BoneRemovedEvent(bone, (bone.Comp.Body.Value, bodyComponent), parent, container.ID);
                RaiseLocalEvent(bone, ev);
                RaiseLocalEvent(bone.Comp.Body.Value, ev);
            }

            bone.Comp.Body = body;
            Dirty(bone);

            SetOrgansBody(body, bone, GetOrgans(bone.AsNullable()));
        }
    }

    #endregion

    #region Public API

    #region GetBones

    /// <summary>
    /// Gets the bones of this body.
    /// </summary>
    public List<Entity<BoneComponent>> GetBones(Entity<BodyComponent?> body, BoneType type = BoneType.None)
    {
        if (!Resolve(body, ref body.Comp))
            return new List<Entity<BoneComponent>>();

        var bones = new List<Entity<BoneComponent>>();
        foreach (var bodyPartSlot in body.Comp.BodyParts.Values)
        {
            if (!TryComp<BodyPartComponent>(bodyPartSlot.BodyPartUid, out var bodyPartComponent))
                continue;

            bones.AddRange(GetBones((bodyPartSlot.BodyPartUid.Value, bodyPartComponent), type));
        }

        return bones;
    }

    /// <summary>
    /// Gets the bones of this body part.
    /// </summary>
    public List<Entity<BoneComponent>> GetBones(Entity<BodyPartComponent?> bodyPart, BoneType type = BoneType.None)
    {
        if (!Resolve(bodyPart, ref bodyPart.Comp))
            return new List<Entity<BoneComponent>>();

        var bones = new List<Entity<BoneComponent>>();
        foreach (var boneSlot in bodyPart.Comp.Bones.Values)
        {
            if (!TryComp<BoneComponent>(boneSlot.BoneUid, out var boneComponent) || !boneComponent.Type.HasFlag(type))
                continue;

            bones.Add((boneSlot.BoneUid.Value, boneComponent));
        }

        return bones;
    }

    /// <summary>
    /// Gets the bones of this entity.
    /// </summary>
    public List<Entity<BoneComponent>> GetBones(EntityUid parent, BoneType type = BoneType.None)
    {
        if (TryComp<BodyComponent>(parent, out var bodyComponent))
            return GetBones((parent, bodyComponent), type);

        if (TryComp<BodyPartComponent>(parent, out var bodyPartComponent))
            return GetBones((parent, bodyPartComponent), type);

        return new List<Entity<BoneComponent>>();
    }

    #endregion

    #region GetBones<T>

    /// <summary>
    /// Gets the bones of this body with the given component.
    /// </summary>
    public List<Entity<BoneComponent, T>> GetBones<T>(Entity<BodyComponent?> body, BoneType type = BoneType.None) where T : IComponent
    {
        var bones = new List<Entity<BoneComponent, T>>();
        foreach (var bone in GetBones(body, type))
        {
            if (!TryComp<T>(bone, out var component))
                continue;

            bones.Add((bone.Owner, bone.Comp, component));
        }

        return bones;
    }

    /// <summary>
    /// Gets the bones of this body part with the given component.
    /// </summary>
    public List<Entity<BoneComponent, T>> GetBones<T>(Entity<BodyPartComponent?> bodyPart, BoneType type = BoneType.None) where T : IComponent
    {
        var bones = new List<Entity<BoneComponent, T>>();
        foreach (var bone in GetBones(bodyPart, type))
        {
            if (!TryComp<T>(bone, out var component))
                continue;

            bones.Add((bone.Owner, bone.Comp, component));
        }

        return bones;
    }

    /// <summary>
    /// Gets the bones of this entity with the given component.
    /// </summary>
    public List<Entity<BoneComponent, T>> GetBones<T>(EntityUid parent, BoneType type = BoneType.None) where T : IComponent
    {
        var bones = new List<Entity<BoneComponent, T>>();
        foreach (var bone in GetBones(parent, type))
        {
            if (!TryComp<T>(bone, out var component))
                continue;

            bones.Add((bone.Owner, bone.Comp, component));
        }

        return bones;
    }

    #endregion

    #region GetBoneSlots

    /// <summary>
    /// Gets the bone slots of this body.
    /// </summary>
    public List<BoneSlot> GetBoneSlots(Entity<BodyComponent?> body, BoneType type = BoneType.None, string? slotId = null)
    {
        if (!Resolve(body, ref body.Comp))
            return new List<BoneSlot>();

        var bodyPartSlots = new List<BoneSlot>();
        foreach (var bodyPart in GetBodyParts(body))
            bodyPartSlots.AddRange(GetBoneSlots(bodyPart.AsNullable(), type, slotId));

        return bodyPartSlots;
    }

    /// <summary>
    /// Gets the bone slots of this body part.
    /// </summary>
    public List<BoneSlot> GetBoneSlots(Entity<BodyPartComponent?> bodyPart, BoneType type = BoneType.None, string? slotId = null)
    {
        if (!Resolve(bodyPart, ref bodyPart.Comp))
            return new List<BoneSlot>();

        var boneSlots = new List<BoneSlot>();
        foreach (var boneSlot in bodyPart.Comp.Bones.Values)
        {
            if (!boneSlot.Type.HasFlag(type) || !string.IsNullOrEmpty(slotId) && boneSlot.Id != slotId)
                continue;

            boneSlots.Add(boneSlot);
        }

        return boneSlots;
    }

    /// <summary>
    /// Gets the bone slots of this entity.
    /// </summary>
    public List<BoneSlot> GetBoneSlots(EntityUid parent, BoneType type = BoneType.None, string? slotId = null)
    {
        if (TryComp<BodyComponent>(parent, out var bodyComponent))
            return GetBoneSlots((parent, bodyComponent), type, slotId);

        if (TryComp<BodyPartComponent>(parent, out var bodyPartComponent))
            return GetBoneSlots((parent, bodyPartComponent), type, slotId);

        return new List<BoneSlot>();
    }

    #endregion

    #region TryCreateBoneSlot

    /// <summary>
    /// Trying to create an bone slot for this body.
    /// </summary>
    public bool TryCreateBoneSlot(Entity<BodyPartComponent?> bodyPart, string boneId, BoneType type)
    {
        if (!Resolve(bodyPart, ref bodyPart.Comp))
            return false;

        var boneSlot = new BoneSlot
        {
            Type = type,
            ContainerSlot = _container.EnsureContainer<ContainerSlot>(bodyPart, GetBoneContainerId(boneId))
        };

        if (!bodyPart.Comp.Bones.TryAdd(boneId, boneSlot))
            return false;

        Dirty(bodyPart);
        return true;
    }

    #endregion

    #region TryAttachBone

    /// <summary>
    /// Trying to attach a bone to this body.
    /// </summary>
    public bool TryAttachBone(Entity<BoneComponent?> body, Entity<BoneComponent?> bone, string slotId)
    {
        if (!Resolve(bone, ref bone.Comp)
            || GetBoneSlots(body, bone.Comp.Type, slotId).FirstOrDefault() is not {} boneSlot
            || boneSlot.ContainerSlot is null)
            return false;

        return _container.Insert(bone.Owner, boneSlot.ContainerSlot);
    }

    /// <summary>
    /// Trying to attach a bone to this body part.
    /// </summary>
    public bool TryAttachBone(Entity<BodyPartComponent?> bodyPart, Entity<BoneComponent?> bone, string slotId)
    {
        if (!Resolve(bone, ref bone.Comp)
            || GetBoneSlots(bodyPart, bone.Comp.Type, slotId).FirstOrDefault() is not {} boneSlot
            || boneSlot.ContainerSlot is null)
            return false;

        return _container.Insert(bone.Owner, boneSlot.ContainerSlot);
    }

    /// <summary>
    /// Trying to attach a bone to this entity.
    /// </summary>
    public bool TryAttachBone(EntityUid parent, Entity<BoneComponent?> bone, string slotId)
    {
        if (!Resolve(bone, ref bone.Comp)
            || GetBoneSlots(parent, bone.Comp.Type, slotId).FirstOrDefault() is not {} boneSlot
            || boneSlot.ContainerSlot is null)
            return false;

        return _container.Insert(bone.Owner, boneSlot.ContainerSlot);
    }

    #endregion

    #endregion
}
