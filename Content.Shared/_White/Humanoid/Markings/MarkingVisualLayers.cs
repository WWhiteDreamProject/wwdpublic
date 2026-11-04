using Robust.Shared.Serialization;

namespace Content.Shared._White.Humanoid.Markings;

[Serializable, NetSerializable]
public enum MarkingVisualLayers : byte
{
    ChestBack,
    Face,
    FacialHair,
    GroinBack,
    Hair,
    HeadBottom,
    HeadSide,
    HeadTop,
    LeftArmTop,
    RightArmTop,
    Snout,
    UndergarmentBottom,
    UndergarmentTop,
}
