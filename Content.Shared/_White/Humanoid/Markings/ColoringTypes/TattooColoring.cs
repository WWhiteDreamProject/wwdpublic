using Content.Shared._White.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Markings.ColoringTypes;

/// <summary>
/// Colors layer in skin color but much darker.
/// </summary>
public sealed partial class TattooColoring : IMarkingColoringStrategy
{
    public Color? GetColor(Dictionary<ProtoId<BodyColorationPrototype>, Color> colors, List<Marking> markings)
    {
        if (!colors.TryGetValue("Skin", out var skinColor))
            return null;

        var newColor = Color.ToHsv(skinColor);
        newColor.Z = .40f;

        return Color.FromHsv(newColor);
    }
}
