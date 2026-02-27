namespace Content.Shared._White.Humanoid.Markings;

/// <summary>
/// A class that defines coloring type and fallback for markings
/// </summary>
[DataDefinition]
public sealed partial class LayerColoringDefinition
{
    [DataField]
    public LayerColoringType? Type = new ColoringTypes.SkinColoring();

    /// <summary>
    /// Coloring types that will be used if main coloring type will return nil
    /// </summary>
    [DataField]
    public List<LayerColoringType> FallbackTypes = new();

    /// <summary>
    /// Color that will be used if coloring type and fallback type will return nil
    /// </summary>
    [DataField]
    public Color FallbackColor = Color.White;

    public Color GetColor(Color? skin, Color? eyes, List<Marking> otherMarkings)
    {
        Color? color = null;

        if (Type != null)
            color = Type.GetColor(skin, eyes, otherMarkings);

        if (color != null)
            return color.Value;

        foreach (var type in FallbackTypes)
        {
            color = type.GetColor(skin, eyes, otherMarkings);
            if (color != null)
                break;
        }

        return color ?? FallbackColor;
    }
}

/// <summary>
/// An abstract class for coloring types
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class LayerColoringType
{
    /// <summary>
    /// Makes output color negative
    /// </summary>
    [DataField]
    public bool Negative { get; private set; }

    public abstract Color? GetCleanColor(Color? skin, Color? eyes, List<Marking> otherMarkings);

    public Color? GetColor(Color? skin, Color? eyes, List<Marking> otherMarkings)
    {
        var color = GetCleanColor(skin, eyes, otherMarkings);

        if (color == null || !Negative)
            return color;

        var reverseColor = color.Value;
        reverseColor.R = 1f-reverseColor.R;
        reverseColor.G = 1f-reverseColor.G;
        reverseColor.B = 1f-reverseColor.B;

        return reverseColor;
    }
}
