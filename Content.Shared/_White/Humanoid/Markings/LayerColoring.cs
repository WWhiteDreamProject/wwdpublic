using Content.Shared._White.Humanoid.Markings.ColoringTypes;
using Content.Shared._White.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Markings;

/// <summary>
/// Defines the logic for determining the color of a specific layer.
/// Supports a primary coloring method, optional fallbacks, and a default constant color.
/// </summary>
[DataDefinition]
public sealed partial class LayerColoringDefinition
{
    /// <summary>
    /// The primary strategy used to calculate the layer's color.
    /// </summary>
    [DataField]
    public IMarkingColoringStrategy? Type = new SimpleColoring();

    /// <summary>
    /// A collection of alternative coloring strategies to attempt if the primary strategy returns null.
    /// </summary>
    [DataField]
    public List<IMarkingColoringStrategy> FallbackStrategies = new();

    /// <summary>
    /// The default color applied if neither the primary type nor any fallback types return a valid color.
    /// </summary>
    [DataField]
    public Color FallbackColor = Color.White;

    /// <summary>
    /// Calculates the final color.
    /// </summary>
    /// <param name="colors">A dictionary mapping coloration prototypes to specific colors.</param>
    /// <param name="markings">A list of existing markings that might influence the color.</param>
    /// <returns>The calculated color.</returns>
    public Color GetColor(Dictionary<ProtoId<BodyColorationPrototype>, Color> colors, List<Marking> markings)
    {
        Color? color = null;

        if (Type != null)
            color = Type.GetColor(colors, markings);

        if (color != null)
            return color.Value;

        foreach (var type in FallbackStrategies)
        {
            color = type.GetColor(colors, markings);
            if (color != null)
                break;
        }

        return color ?? FallbackColor;
    }
}
