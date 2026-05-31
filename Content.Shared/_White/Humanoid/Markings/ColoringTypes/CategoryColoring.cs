using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Humanoid.Markings.ColoringTypes;

/// <summary>
/// A strategy for determining a marking's color based on specific marking category.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class CategoryColoring : IMarkingColoringStrategy
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [DataField(required: true)]
    public ProtoId<MarkingCategoryPrototype> Category;

    public Color? GetColor(Dictionary<ProtoId<BodyColorationPrototype>, Color> colors, List<Marking> markings)
    {
        foreach (var marking in markings)
        {
            if (!_prototype.TryIndex(marking.Id, out var markingPrototype))
                continue;

            if (markingPrototype.Category != Category)
                continue;

            return marking.Color;
        }

        return null;
    }
}
