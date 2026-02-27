using System.Linq;

namespace Content.Shared._White.Humanoid.Markings.ColoringTypes;

/// <summary>
/// Colors marking in color of first defined marking from specified category (in e.x. from Hair category)
/// </summary>
public sealed partial class CategoryColoring : LayerColoringType
{
    [DataField(required: true)]
    public Enum Category;

    public override Color? GetCleanColor(Color? skin, Color? eyes, List<Marking> otherMarkings)
    {
        return otherMarkings.Count > 0 ? otherMarkings[0].MarkingColors.FirstOrDefault() : null;
    }
}
