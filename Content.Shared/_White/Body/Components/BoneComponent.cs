using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Body.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BoneComponent : Component
{
    [DataField("boneType")] // It can't support the "type" tag.
    public BoneType Type = BoneType.None;

    [DataField]
    public Dictionary<string, OrganSlot> Organs = new();

    /// <summary>
    /// Relevant body this bone is attached to.
    /// </summary>
    [DataField]
    public EntityUid? Body;

    /// <summary>
    /// Parent body part for this bone.
    /// </summary>
    [DataField]
    public EntityUid? Parent;
}

/// <summary>
/// Defines various types of bones in the skeletal system.
/// Uses the [Flags] attribute to allow for combining multiple bone types.
/// </summary>
[Flags]
public enum BoneType
{
    None = 0,

    /// <summary>
    /// The cranium, the part of the skull that encloses the brain.
    /// </summary>
    Сranium = 1 << 0,

    /// <summary>
    /// The thorax, consisting of the rib cage and sternum, protecting the heart and lungs.
    /// </summary>
    Thorax = 1 << 1,

    /// <summary>
    /// The coxae, the hip bones (pelvic bones).
    /// </summary>
    Coxae = 1 << 2,

    /// <summary>
    /// The humerus, the long bone in the upper arm.
    /// </summary>
    Humerus = 1 << 3,

    /// <summary>
    /// The antebrachii, the bones of the forearm (radius and ulna).
    /// </summary>
    Antebrachii = 1 << 4,

    /// <summary>
    /// The manus, the bones of the hand (carpals, metacarpals, phalanges).
    /// </summary>
    Manus = 1 << 5,

    /// <summary>
    /// The femur, the long bone of the thigh.
    /// </summary>
    Femur = 1 << 6,

    /// <summary>
    /// The crus, the bones of the lower leg (tibia and fibula).
    /// </summary>
    Crus = 1 << 7,

    /// <summary>
    /// The pedis, the bones of the foot (tarsals, metatarsals, phalanges).
    /// </summary>
    Pedis = 1 << 8,
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BoneSlot
{
    public BoneSlot() { }

    public BoneSlot(BoneSlot other)
    {
        CopyFrom(other);
    }

    public BoneSlot(BoneType type, Dictionary<string, OrganSlot> organs, EntProtoId? startingBone)
    {
        Type = type;
        Organs = organs;
        StartingBone = startingBone;
    }

    [DataField]
    public BoneType Type = BoneType.None;

    [DataField]
    public Dictionary<string, OrganSlot> Organs = new();

    [DataField(readOnly: true)]
    public EntProtoId? StartingBone;

    [ViewVariables, NonSerialized]
    public ContainerSlot? ContainerSlot;

    public string? Id => ContainerSlot?.ID;
    public bool HasBone => ContainerSlot?.ContainedEntity != null;
    public EntityUid? BoneUid => ContainerSlot?.ContainedEntity;

    public void CopyFrom(BoneSlot other)
    {
        Type = other.Type;
        Organs = other.Organs.ToDictionary(x => x.Key, x => new OrganSlot(x.Value));
        StartingBone = other.StartingBone;
    }
}

[Serializable, NetSerializable]
public sealed class BoneComponentState : ComponentState
{
    public BoneComponentState(BoneComponent component, EntityManager entityManager)
    {
        Type = component.Type;

        Organs = new();
        foreach (var (stateKey, stateSlot) in component.Organs)
            Organs.Add(stateKey, stateSlot);

        Body = entityManager.GetNetEntity(component.Body);
        Parent = entityManager.GetNetEntity(component.Parent);
    }

    public readonly BoneType Type;

    public readonly Dictionary<string, OrganSlot> Organs;

    public readonly NetEntity? Body;

    public readonly NetEntity? Parent;
}
