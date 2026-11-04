using Content.Shared._White.Appearance;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Humanoid.Markings.UserInterface;

[Serializable, NetSerializable]
public sealed class MarkingModifierMarkingSetMessage(
    Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> markings)
    : BoundUserInterfaceMessage
{
    public Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> Markings { get; } = markings;
}

[Serializable, NetSerializable]
public sealed class MarkingModifierState(
    Dictionary<Enum, BodyAppearanceData> appearanceData,
    Dictionary<Enum, MarkingData> markingsData,
    Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> markings)
    : BoundUserInterfaceState
{
    public Dictionary<Enum, BodyAppearanceData> AppearanceData { get; } = appearanceData;
    public Dictionary<Enum, MarkingData> MarkingsData { get; } = markingsData;
    public Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> Markings { get; } = markings;
}

[Serializable, NetSerializable]
public enum MarkingModifierKey
{
    Key,
}
