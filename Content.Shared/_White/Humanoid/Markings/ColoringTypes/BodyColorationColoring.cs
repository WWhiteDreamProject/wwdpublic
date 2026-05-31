using Content.Shared._White.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Humanoid.Markings.ColoringTypes;

/// <summary>
/// A strategy for determining a marking's color based on specific body coloration.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class BodyColorationColoring : IMarkingColoringStrategy
{
    [DataField(required: true)]
    public ProtoId<BodyColorationPrototype> BodyColoration;

    public Color? GetColor(Dictionary<ProtoId<BodyColorationPrototype>, Color> colors, List<Marking> markings)
    {
        if (!colors.TryGetValue(BodyColoration, out var color))
            return null;

        return color;
    }
}
