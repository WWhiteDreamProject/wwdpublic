using Content.Shared._White.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Markings.ColoringTypes;

/// <summary>
/// Defines custom marking color calculation strategies.
/// </summary>
public interface IMarkingColoringStrategy
{
    /// <summary>
    /// Resolves the color.
    /// </summary>
    /// <param name="colors">Current body coloration mapping.</param>
    /// <param name="markings">List of active markings.</param>
    /// <returns>The processed color or null if resolution failed.</returns>
    public Color? GetColor(Dictionary<ProtoId<BodyColorationPrototype>, Color> colors, List<Marking> markings);
}
