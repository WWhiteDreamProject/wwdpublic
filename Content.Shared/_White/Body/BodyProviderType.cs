namespace Content.Shared._White.Body;

/// <summary>
/// Defines various types of body provider that can be manipulated.
/// </summary>
[Flags]
public enum BodyProviderType
{
    None = 0,

    #region Types

    Part = 1 << 0,
    Organ = 1 << 1,
    Bone = 1 << 2,

    #endregion Types

    #region Sides

    Left = 1 << 3,
    Middle = 1 << 4,
    Right = 1 << 5,

    #region Combined

    AllSides = Left | Middle | Right,

    #endregion Combined

    #endregion Sides

    #region Body parts

    Head = Part | (1 << 6),
    Chest = Part | (1 << 7),
    Groin = Part | (1 << 8),
    Arm = Part | (1 << 9),
    Hand = Part | (1 << 10),
    Leg = Part | (1 << 11),
    Foot = Part | (1 << 12),
    Tail = Part | (1 << 13),
    Wings = Part | (1 << 14),
    Mouth = Part | (1 << 15),

    #region Combined

    #region Arms

    LeftArm = Left | Arm,
    MiddleArm = Middle | Arm,
    RightArm = Right | Arm,
    Arms = Arm | AllSides,

    #endregion Arms

    #region Hands

    LeftHand = Left | Hand,
    MiddleHand = Middle | Hand,
    RightHand = Right | Hand,
    Hands = Hand | AllSides,

    #endregion Hands

    #region Legs

    LeftLeg = Left | Leg,
    MiddleLeg = Middle | Leg,
    RightLeg = Right | Leg,
    Legs = Leg | AllSides,

    #endregion Legs

    #region Foots

    LeftFoot = Left | Foot,
    MiddleFoot = Middle | Foot,
    RightFoot = Right | Foot,
    Foots = Foot | AllSides,

    #endregion Foots

    #region Other

    Torso = Chest | Groin,
    FullLeftArm  = LeftArm | LeftHand,
    FullRightArm  = RightArm | RightHand,
    FullLeftLeg  = LeftLeg | LeftFoot,
    FullRightLeg  = RightLeg | RightFoot,
    FullArms  = Arms | Hands,
    FullLegs  = Legs | Foots,
    Limbs = FullArms | FullLegs | Tail,
    AllParts = Head | Torso | Limbs,

    #endregion Other

    #endregion Combined

    #endregion Body parts

    #region Organs

    Brain = Organ | (1 << 6),
    Heart = Organ | (1 << 7),
    Eyes = Organ | (1 << 8),
    Tongue = Organ | (1 << 9),
    Appendix = Organ | (1 << 10),
    Ears = Organ | (1 << 11),
    Lungs = Organ | (1 << 12),
    Stomach = Organ | (1 << 13),
    Liver = Organ | (1 << 14),
    Kidneys = Organ | (1 << 15),
    Gland = Organ | (1 << 16),
    Specific = Organ | (1 << 17),

    #region Combined

    #region Other

    Core = Brain | Heart | Stomach | Liver | Kidneys,
    AllOrgans = Core | Eyes | Tongue | Appendix | Ears | Lungs | Gland | Specific,

    #endregion Other

    #endregion Combined

    #endregion Organs

    #region Bones

    Сranium = Bone | (1 << 6),
    Thorax = Bone | (1 << 7),
    Coxae = Bone | (1 << 8),
    Antebrachium = Bone | (1 << 9),
    Manus = Bone | (1 << 10),
    Crus = Bone | (1 << 11),
    Pedis = Bone | (1 << 12),

    #region Combined

    #region Antebrachium

    LeftAntebrachium = Left | Antebrachium,
    MiddleAntebrachium = Middle | Antebrachium,
    RightAntebrachium = Right | Antebrachium,

    #endregion Antebrachium

    #region Manus

    LeftManus = Left | Manus,
    MiddleManus = Middle | Manus,
    RightManus = Right | Manus,

    #endregion Manus

    #region Crus

    LeftCrus = Left | Crus,
    MiddleCrus = Middle | Crus,
    RightCrus = Right | Crus,

    #endregion Crus

    #region Pedis

    LeftPedis = Left | Pedis,
    MiddlePedis = Middle | Pedis,
    RightPedis = Right | Pedis,

    #endregion Pedis

    #region Other

    AllBones = Сranium | Thorax | Coxae | Antebrachium | Manus | Crus | Pedis | AllSides,

    #endregion Other

    #endregion Combined

    #endregion Bones

    #region Specific

    #region Combined

    Face = Head | Eyes | Mouth,
    All = ~None,

    #endregion Combined

    #endregion Specific
}
