using Robust.Shared.GameStates;

namespace Content.Shared._White.TargetDoll;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TargetDollComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public BodyPart SelectedBodyPart = BodyPart.Chest;
}

/// <summary>
/// Different parts of the body that can be manipulated.
/// </summary>
[Flags]
public enum BodyPart
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
    /// Eyes.
    /// </summary>
    Eyes = 1 << 11,

    /// <summary>
    /// Mouth.
    /// </summary>
    Mouth = 1 << 12,


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
    /// All Limbs.
    /// </summary>
    AllLimbs = Arm | Hand | Leg | Foot | Tail,

    /// <summary>
    /// All Parts.
    /// </summary>
    All = Torso | Face | AllLimbs,
}
