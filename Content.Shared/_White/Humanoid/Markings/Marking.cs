using System.Linq;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._White.Humanoid.Markings;

/// <summary>
/// Represents a marking ID and its colors
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial record struct Marking
{
    /// <summary>
    /// The <see cref="MarkingPrototype"/> referred to by this marking.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MarkingPrototype> MarkingId;

    /// <summary>
    /// The colors taken on by the marking
    /// </summary>
    [DataField]
    public List<Color> MarkingColors { get; private set; } = new();

    /// <summary>
    /// Whether the marking is forced regardless of points
    /// </summary>
    public bool Forced;

    public Marking(ProtoId<MarkingPrototype> markingId, List<Color> colors)
    {
        MarkingId = markingId;
        MarkingColors = colors;
    }

    public Marking(ProtoId<MarkingPrototype> markingId, int colorsCount) : this(
        markingId,
        Enumerable.Repeat(Color.White, colorsCount).ToList()) { }

    public bool Equals(Marking other)
    {
        return MarkingId.Equals(other.MarkingId)
            && MarkingColors.SequenceEqual(other.MarkingColors)
            && Forced.Equals(other.Forced);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MarkingId, MarkingColors, Forced);
    }

    public Marking WithColor(Color color)
    {
        return this with { MarkingColors = Enumerable.Repeat(color, MarkingColors.Count).ToList(), };
    }

    public Marking WithColorAt(int index, Color color)
    {
        var newColors = MarkingColors.ShallowClone();
        newColors[index] = color;
        return this with { MarkingColors = newColors, };
    }

    /// <summary>
    /// Returns list of colors for marking layers
    /// </summary>
    public static List<Color> GetMarkingLayerColors(MarkingPrototype prototype, Color? skinColor, Color? eyeColor, List<Marking> otherMarkings)
    {
        var colors = new List<Color>();

        // Coloring from default properties
        var defaultColor = prototype.Coloring.Default.GetColor(skinColor, eyeColor, otherMarkings);

        if (prototype.Coloring.Layers == null)
        {
            // If layers is not specified, then every layer must be default
            for (var i = 0; i < prototype.Markings.Count; i++)
                colors.Add(defaultColor);

            return colors;
        }

        // If some layers are specified.
        foreach (var marking in prototype.Markings)
        {
            // Getting layer name
            var name = marking.Sprite switch
            {
                SpriteSpecifier.Rsi rsi => rsi.RsiState,
                SpriteSpecifier.Texture texture => texture.TexturePath.Filename,
                _ => null
            };

            if (name == null || !prototype.Coloring.Layers.TryGetValue(name, out var layerColoring))
            {
                colors.Add(defaultColor);
                continue;
            }

            var markingColor = layerColoring.GetColor(skinColor, eyeColor, otherMarkings);
            colors.Add(markingColor);
        }

        return colors;
    }
}

/// <summary>
/// Default colors for marking
/// </summary>
[DataDefinition]
public sealed partial class MarkingColors
{
    /// <summary>
    /// Coloring properties that will be used on any unspecified layer
    /// </summary>
    [DataField(readOnly:true)]
    public LayerColoringDefinition Default = new ();

    /// <summary>
    /// Layers with their own coloring type and properties
    /// </summary>
    [DataField(readOnly:true)]
    public Dictionary<string, LayerColoringDefinition>? Layers;
}
