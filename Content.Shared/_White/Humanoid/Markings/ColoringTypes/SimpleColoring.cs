using Content.Shared._White.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Markings.ColoringTypes;

/// <summary>
/// A coloring strategy that applies a fixed, specified color to a marking.
/// </summary>
public sealed partial class SimpleColoring : IMarkingColoringStrategy
{
    [DataField(required: true)]
    public Color Color = Color.White;

    public Color? GetColor(Dictionary<ProtoId<BodyColorationPrototype>, Color> colors, List<Marking> markings)
    {
        return Color;
    }
}
