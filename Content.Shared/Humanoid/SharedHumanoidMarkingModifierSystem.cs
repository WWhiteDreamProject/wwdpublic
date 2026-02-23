using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

[Serializable, NetSerializable]
public enum HumanoidMarkingModifierKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class HumanoidMarkingModifierMarkingSetMessage : BoundUserInterfaceMessage
{
    public MarkingSet MarkingSet { get; }
    public bool ResendState { get; }

    public HumanoidMarkingModifierMarkingSetMessage(MarkingSet set, bool resendState)
    {
        MarkingSet = set;
        ResendState = resendState;
    }
}

[Serializable, NetSerializable]
public sealed class HumanoidMarkingModifierState : BoundUserInterfaceState
{
    // TODO just use the component state, remove the BUI state altogether.
    public HumanoidMarkingModifierState(
        MarkingSet markingSet,
        string species,
        string bodyType, // WD EDIT
        Sex sex,
        Color skinColor,
        Color eyeColor // WD EDIT
    )
    {
        MarkingSet = markingSet;
        Species = species;
        BodyType = bodyType; // WD EDIT
        Sex = sex;
        SkinColor = skinColor;
        EyeColor = eyeColor; // WD EDIT
    }

    public MarkingSet MarkingSet { get; }
    public string Species { get; }
    public string BodyType { get; } // WD EDIT
    public Sex Sex { get; }
    public Color SkinColor { get; }
    public Color EyeColor { get; }
    public Color? HairColor { get; }
    public Color? FacialHairColor { get; }
}
