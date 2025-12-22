using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Body.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BodyPartComponent : Component
{
    /// <summary>
    /// The type of this body part
    /// </summary>
    [DataField("bodyPartType")] // It can't support the "type" tag.
    public BodyPartType Type = BodyPartType.None;

    /// <summary>
    /// Child body parts attached to this body part.
    /// </summary>
    [DataField]
    public Dictionary<string, BodyPartSlot> BodyParts = new();

    /// <summary>
    /// Bones attached to this body part.
    /// </summary>
    [DataField]
    public Dictionary<string, BoneSlot> Bones = new();

    /// <summary>
    /// Organs attached to this body part.
    /// </summary>
    [DataField]
    public Dictionary<string, OrganSlot> Organs = new();

    /// <summary>
    /// Parent body for this part.
    /// </summary>
    [ViewVariables]
    public EntityUid? Body;

    /// <summary>
    /// Parent body part for this part.
    /// </summary>
    [ViewVariables]
    public EntityUid? Parent;
}

/// <summary>
/// Defines various types of body parts that can be manipulated.
/// </summary>
[Flags]
public enum BodyPartType
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


    // --- Main parts ---


    /// <summary>
    /// Head.
    /// </summary>
    Head = 1 << 3,

    /// <summary>
    /// Chest.
    /// </summary>
    Chest = 1 << 4,

    /// <summary>
    /// Groin.
    /// </summary>
    Groin = 1 << 5,

    /// <summary>
    /// Arm.
    /// </summary>
    Arm = 1 << 6,

    /// <summary>
    /// Hand.
    /// </summary>
    Hand = 1 << 7,

    /// <summary>
    /// Leg.
    /// </summary>
    Leg = 1 << 8,

    /// <summary>
    /// Foot.
    /// </summary>
    Foot = 1 << 9,

    /// <summary>
    /// Tail.
    /// </summary>
    Tail = 1 << 10,

    /// <summary>
    /// Wings.
    /// </summary>
    Wings = 1 << 11,

    /// <summary>
    /// Eyes.
    /// </summary>
    Eyes = 1 << 12,

    /// <summary>
    /// Mouth.
    /// </summary>
    Mouth = 1 << 13,


    // --- Combined parts ---

    // -- Arms --

    /// <summary>
    /// Left Arm.
    /// </summary>
    LeftArm = Left | Arm,

    /// <summary>
    /// Middle Arm.
    /// </summary>
    MiddleArm = Middle | Arm,

    /// <summary>
    /// Right Arm.
    /// </summary>
    RightArm = Right | Arm,

    // -- Hands --

    /// <summary>
    /// Left Hand.
    /// </summary>
    LeftHand = Left | Hand,

    /// <summary>
    /// Middle Hand.
    /// </summary>
    MiddleHand = Middle | Hand,

    /// <summary>
    /// Right Hand.
    /// </summary>
    RightHand = Right | Hand,

    // -- Legs --

    /// <summary>
    /// Left Leg.
    /// </summary>
    LeftLeg = Left | Leg,

    /// <summary>
    /// Middle Leg.
    /// </summary>
    MiddleLeg = Middle | Leg,

    /// <summary>
    /// Right Leg.
    /// </summary>
    RightLeg = Right | Leg,

    // -- Foots --

    /// <summary>
    /// Left Foot
    /// </summary>
    LeftFoot = Left | Foot,

    /// <summary>
    /// Middle Foot
    /// </summary>
    MiddleFoot = Middle | Foot,

    /// <summary>
    /// Right Foot
    /// </summary>
    RightFoot = Right | Foot,

    // -- Other --

    /// <summary>
    /// Torso.
    /// </summary>
    Torso = Chest | Groin,

    /// <summary>
    /// Face.
    /// </summary>
    Face = Head | Eyes | Mouth,

    /// <summary>
    /// All arms.
    /// </summary>
    Arms = Arm | Left | Middle | Right,

    /// <summary>
    /// All hands.
    /// </summary>
    Hands = Hand | Left | Middle | Right,

    /// <summary>
    /// All legs.
    /// </summary>
    Legs = Leg | Left | Middle | Right,

    /// <summary>
    /// All foots.
    /// </summary>
    Foots = Foot | Left | Middle | Right,

    /// <summary>
    /// The entire left limb, including the arm and hand.
    /// </summary>
    FullLeftArm  = LeftArm | LeftHand,

    /// <summary>
    /// The entire right limb, including the arm and hand.
    /// </summary>
    FullRightArm  = RightArm | RightHand,

    /// <summary>
    /// The entire left limb, including the leg and foot.
    /// </summary>
    FullLeftLeg  = LeftLeg | LeftFoot,

    /// <summary>
    /// The entire right limb, including the leg and foot.
    /// </summary>
    FullRightLeg  = RightLeg | RightFoot,

    /// <summary>
    /// All upper limbs, including arms and hands.
    /// </summary>
    FullArms  = Arms | Hands,

    /// <summary>
    /// All lower limbs, including the legs and foots.
    /// </summary>
    FullLegs  = Legs | Foots,

    /// <summary>
    /// All Limbs.
    /// </summary>
    AllLimbs = FullArms | FullLegs | Tail,

    /// <summary>
    /// All Parts.
    /// </summary>
    All = Torso | Face | AllLimbs
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BodyPartSlot
{
    public BodyPartSlot() { }

    public BodyPartSlot(BodyPartSlot other)
    {
        CopyFrom(other);
    }

    public BodyPartSlot(
        BodyPartType type,
        HashSet<string> connections,
        Dictionary<string, BoneSlot> bones,
        Dictionary<string, OrganSlot> organs,
        EntProtoId? startingBodyPart
        )
    {
        Type = type;
        Connections = connections;
        Bones = bones;
        Organs = organs;
        StartingBodyPart = startingBodyPart;
    }

    [DataField]
    public BodyPartType Type = BodyPartType.None;

    [DataField]
    public HashSet<string> Connections = new();

    [DataField]
    public Dictionary<string, BoneSlot> Bones = new();

    [DataField]
    public Dictionary<string, OrganSlot> Organs = new();

    [DataField(readOnly: true)]
    public EntProtoId? StartingBodyPart;

    [ViewVariables, NonSerialized]
    public ContainerSlot? ContainerSlot;

    public string? Id => ContainerSlot?.ID;
    public bool HasBodyPart => ContainerSlot?.ContainedEntity != null;
    public EntityUid? BodyPartUid => ContainerSlot?.ContainedEntity;

    public void CopyFrom(BodyPartSlot other)
    {
        Type = other.Type;
        Connections = other.Connections;
        Bones = other.Bones.ToDictionary(x => x.Key, x => new BoneSlot(x.Value));
        Organs = other.Organs.ToDictionary(x => x.Key, x => new OrganSlot(x.Value));
        StartingBodyPart = other.StartingBodyPart;
    }
}

[Serializable, NetSerializable]
public sealed class BodyPartComponentState : ComponentState
{
    public BodyPartComponentState(BodyPartComponent component, EntityManager entityManager)
    {
        Type = component.Type;

        BodyParts = new();
        foreach (var (stateKey, stateSlot) in component.BodyParts)
            BodyParts.Add(stateKey, stateSlot);

        Bones = new();
        foreach (var (stateKey, stateSlot) in component.Bones)
            Bones.Add(stateKey, stateSlot);

        Organs = new();
        foreach (var (stateKey, stateSlot) in component.Organs)
            Organs.Add(stateKey, stateSlot);

        Body = entityManager.GetNetEntity(component.Body);
        Parent = entityManager.GetNetEntity(component.Parent);
    }

    public readonly BodyPartType Type;

    public readonly Dictionary<string, BodyPartSlot> BodyParts;

    public readonly Dictionary<string, BoneSlot> Bones;

    public readonly Dictionary<string, OrganSlot> Organs;

    public readonly NetEntity? Body;

    public readonly NetEntity? Parent;
}
