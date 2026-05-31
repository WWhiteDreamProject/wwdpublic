using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._White.Humanoid.Markings;

/// <summary>
/// Represents a marking ID and its colors.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial record struct Marking
{
    /// <summary>
    /// The color of this marking.
    /// </summary>
    [DataField]
    public Color Color { get; private set; } = Color.White;

    /// <summary>
    /// The layer of this marking.
    /// </summary>
    [DataField(required: true)]
    public Enum Layer { get; private set; }

    /// <summary>
    /// The <see cref="MarkingPrototype"/> referred to by this marking.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MarkingPrototype> Id { get; private set; }

    /// <summary>
    /// The sprite of this marking.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Sprite { get; private set; }

    /// <summary>
    /// Whether the marking is forced regardless of points.
    /// </summary>
    public bool Forced;

    public Marking(Enum layer, ProtoId<MarkingPrototype> id, SpriteSpecifier sprite, Color? color = null)
    {
        Color = color ?? Color.White;
        Layer = layer;
        Id = id;
        Sprite = sprite;
    }

    public bool Equals(Marking other)
    {
        return Color.Equals(other.Color)
            && Layer.Equals(other.Layer)
            && Id.Equals(other.Id)
            && Sprite.Equals(other.Sprite)
            && Forced.Equals(other.Forced);
    }

    public Marking WithColor(Color color)
    {
        return this with { Color = color };
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Color, Layer, Id, Sprite, Forced);
    }

    /// <summary>
    /// Returns list of colors for marking layers
    /// </summary>
    public static List<Color> GetMarkingColors(MarkingPrototype prototype, Dictionary<ProtoId<BodyColorationPrototype>, Color> bodyColors, List<Marking> otherMarkings)
    {
        var colors = new List<Color>();

        var defaultColor = prototype.Coloring.Default.GetColor(bodyColors, otherMarkings);

        if (prototype.Coloring.Layers == null)
        {
            for (var i = 0; i < prototype.Markings.Count; i++)
                colors.Add(defaultColor);

            return colors;
        }

        foreach (var marking in prototype.Markings)
        {
            var name = marking.Sprite switch
            {
                SpriteSpecifier.Rsi rsi => rsi.RsiState,
                SpriteSpecifier.Texture texture => texture.TexturePath.Filename,
                _ => string.Empty,
            };

            if (!prototype.Coloring.Layers.TryGetValue(name, out var layerColoring))
            {
                colors.Add(defaultColor);
                continue;
            }

            var markingColor = layerColoring.GetColor(bodyColors, otherMarkings);
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
