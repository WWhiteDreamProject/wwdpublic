using Content.Shared._White.Appearance.Prototypes;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Humanoid.Markings.Coloring;

/// <summary>
/// A coloring strategy that determinate a marking's color based on specific marking category.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class CategoryColoring : IMarkingColoringStrategy
{
    /// <summary>
    /// The target category to look for within the markings list.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MarkingCategoryPrototype> Category;

    /// <inheritdoc />
    public Color? GetColor(Dictionary<ProtoId<BodyColorGroupPrototype>, Color> colorGroups, List<Marking> markings)
    {
        foreach (var marking in markings)
        {
            if (marking.Category != Category)
                continue;

            return marking.Color;
        }

        return null;
    }
}
