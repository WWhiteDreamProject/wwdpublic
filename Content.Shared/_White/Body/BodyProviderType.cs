namespace Content.Shared._White.Body;

/// <summary>
/// Defines various types of body provider that can be manipulated.
/// </summary>
[Flags]
public enum BodyProviderType
{
    None = 0,

    #region Types

    Bone = 1 << 0,
    Organ = 1 << 1,
    Part = 1 << 2,
    Specific = 1 << 3,

    #endregion Types

    #region Sides

    Left = 1 << 4,
    Middle = 1 << 5,
    Right = 1 << 6,

    #region Combined

    AllSides = Left | Middle | Right,

    #endregion Combined

    #endregion Sides

    #region Bones

    Antebrachium = Bone | (1 << 7),
    Coxae = Bone | (1 << 8),
    Сranium = Bone | (1 << 9),
    Crus = Bone | (1 << 10),
    Manus = Bone | (1 << 11),
    Pedis = Bone | (1 << 12),
    Thorax = Bone | (1 << 13),

    #region Combined

    #region Antebrachium

    Antebrachia = Antebrachium | AllSides,
    LeftAntebrachium = Left | Antebrachium,
    MiddleAntebrachium = Middle | Antebrachium,
    RightAntebrachium = Right | Antebrachium,

    #endregion Antebrachium

    #region Crus

    Сrura = Crus | AllSides,
    LeftCrus = Crus | Left,
    MiddleCrus = Crus | Middle,
    RightCrus = Crus | Right,

    #endregion Crus

    #region Manus

    Manuum = Manus | AllSides,
    LeftManus = Manus | Left,
    MiddleManus = Manus | Middle,
    RightManus = Manus | Right,

    #endregion Manus

    #region Pedis

    Pedum = Pedis | AllSides,
    LeftPedis = Pedis | Left,
    MiddlePedis = Pedis | Middle,
    RightPedis = Pedis | Right,

    #endregion Pedis

    AllBones = Antebrachia | Coxae | Сranium | Сrura | Manuum | Pedum | Thorax,

    #endregion Combined

    #endregion Bones

    #region Organs

    Appendix = Organ | (1 << 7),
    Brain = Organ | (1 << 8),
    Ears = Organ | (1 << 9),
    Eyes = Organ | (1 << 10),
    Gland = Organ | (1 << 11),
    Heart = Organ | (1 << 12),
    Kidneys = Organ | (1 << 13),
    Liver = Organ | (1 << 14),
    Lungs = Organ | (1 << 15),
    SpecificOrgan = Organ | (1 << 16),
    Stomach = Organ | (1 << 17),
    Tongue = Organ | (1 << 18),

    #region Combined

    Core = Brain | Heart | Kidneys | Liver | Stomach,
    AllOrgans = Appendix | Ears | Eyes | Gland | Lungs | Tongue,

    #endregion Combined

    #endregion Organs

    #region Parts

    Arm = Part | (1 << 7),
    Chest = Part | (1 << 8),
    Foot = Part | (1 << 9),
    Groin = Part | (1 << 10),
    Hand = Part | (1 << 11),
    Head = Part | (1 << 12),
    Leg = Part | (1 << 13),
    Mouth = Part | (1 << 14),
    Tail = Part | (1 << 15),
    Wings = Part | (1 << 16),

    #region Combined

    #region Arms

    Arms = Arm | AllSides,
    LeftArm = Arm | Left,
    MiddleArm = Arm | Middle,
    RightArm = Arm | Right,

    #endregion Arms

    #region Foots

    Foots = Foot | AllSides,
    LeftFoot = Foot | Left,
    MiddleFoot = Foot | Middle,
    RightFoot = Foot | Right,

    #endregion Foots

    #region Hands

    Hands = Hand | AllSides,
    LeftHand = Hand | Left,
    MiddleHand = Hand | Middle,
    RightHand = Hand | Right,

    #endregion Hands

    #region Legs

    Legs = Leg | AllSides,
    LeftLeg = Leg | Left,
    MiddleLeg = Leg | Middle,
    RightLeg = Leg | Right,

    #endregion Legs

    AllParts = Head | Torso | Limbs,
    FullArms  = Arms | Hands,
    FullLeftArm  = LeftArm | LeftHand,
    FullLeftLeg  = LeftLeg | LeftFoot,
    FullLegs  = Legs | Foots,
    FullRightArm  = RightArm | RightHand,
    FullRightLeg  = RightLeg | RightFoot,
    Limbs = FullArms | FullLegs | Tail,
    Torso = Chest | Groin,

    #endregion Combined

    #endregion Body parts

    #region Combined

    All = ~None,
    Face = Head | Eyes | Mouth,

    #endregion Combined
}
