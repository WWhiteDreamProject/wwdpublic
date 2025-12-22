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
/// Defines various types of bones that can be manipulated.
/// </summary>
[Flags]
public enum BoneType
{
    None = 0,

    // --- Sides ---


    /// <summary>
    /// Left side.
    /// </summary>
    Left = 1 << 0,

    /// <summary>
    /// Middle part.
    /// </summary>
    Middle = 1 << 1,

    /// <summary>
    /// Right part.
    /// </summary>
    Right = 1 << 2,


    // --- Main bones ---


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
    /// The antebrachium, the bones of the forearm (radius and ulna), but within the framework of one bone - one body part also includes Humerus.
    /// </summary>
    Antebrachium = 1 << 3,

    /// <summary>
    /// The manus, the bones of the hand (carpals, metacarpals, phalanges).
    /// </summary>
    Manus = 1 << 4,

    /// <summary>
    /// The crus, the bones of the lower leg (tibia and fibula), but within the framework of one bone - one body part also includes Femur.
    /// </summary>
    Crus = 1 << 5,

    /// <summary>
    /// The pedis, the bones of the foot (tarsals, metatarsals, phalanges).
    /// </summary>
    Pedis = 1 << 6,


    // --- Combined bones ---

    // -- Antebrachium --

    /// <summary>
    /// Left Antebrachium.
    /// </summary>
    LeftAntebrachium = Left | Antebrachium,

    /// <summary>
    /// Middle Antebrachium.
    /// </summary>
    MiddleAntebrachium = Middle | Antebrachium,

    /// <summary>
    /// Right Antebrachium.
    /// </summary>
    RightAntebrachium = Right | Antebrachium,

    // -- Manus --

    /// <summary>
    /// Left Manus.
    /// </summary>
    LeftManus = Left | Manus,

    /// <summary>
    /// Middle Manus.
    /// </summary>
    MiddleManus = Middle | Manus,

    /// <summary>
    /// Right Manus.
    /// </summary>
    RightManus = Right | Manus,

    // -- Crus --

    /// <summary>
    /// Left Crus.
    /// </summary>
    LeftCrus = Left | Crus,

    /// <summary>
    /// Middle Crus.
    /// </summary>
    MiddleCrus = Middle | Crus,

    /// <summary>
    /// Right Crus.
    /// </summary>
    RightCrus = Right | Crus,

    // -- Pedis --

    /// <summary>
    /// Left Pedis.
    /// </summary>
    LeftPedis = Left | Pedis,

    /// <summary>
    /// Middle Pedis.
    /// </summary>
    MiddlePedis = Middle | Pedis,

    /// <summary>
    /// Right Pedis.
    /// </summary>
    RightPedis = Right | Pedis,
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
