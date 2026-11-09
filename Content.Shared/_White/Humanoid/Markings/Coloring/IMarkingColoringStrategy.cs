using Content.Shared._White.Appearance.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Markings.Coloring;

/// <summary>
/// Defines a strategy for calculating the color of a marking layer.
/// </summary>
public interface IMarkingColoringStrategy
{
    /// <summary>
    /// Resolves the color for a specific marking.
    /// </summary>
    /// <param name="colorGroups">A dictionary mapping body color groups ids to their current color.</param>
    /// <param name="markings">The list of all currently active marking.</param>
    /// <returns>The calculated color if the resolution was successful; otherwise, null.</returns>
    public Color? GetColor(Dictionary<ProtoId<BodyColorGroupPrototype>, Color> colorGroups, List<Marking> markings);
}
