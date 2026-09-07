using Content.Shared._White.Humanoid.Markings.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._White.Humanoid.Markings;

/// <summary>
/// Represents an instance of a marking applied to a humanoid.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public partial record struct Marking
{
    /// <summary>
    /// Determines whether this marker overrides the appearance provider's sprite.
    /// </summary>
    [DataField]
    public bool OverrideAppearance { get; private set; }

    /// <summary>
    /// The color tint applied to this marking.
    /// </summary>
    [DataField]
    public Color Color { get; private set; } = Color.White;

    /// <summary>
    /// The specific layer where this marking is rendered.
    /// </summary>
    [DataField(required: true)]
    public Enum Layer { get; private set; }

    /// <summary>
    /// The category to which this marking belongs.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MarkingCategoryPrototype> Category { get; private set; }

    /// <summary>
    /// The reference to the <see cref="MarkingPrototype"/> defining this marking's base data.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MarkingPrototype> Id { get; private set; }

    /// <summary>
    /// The specific sprite resource used for this marking.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Sprite { get; private set; }

    /// <summary>
    /// The index associated with this marking.
    /// </summary>
    [DataField(required: true)]
    public string Index { get; private set; }

    /// <summary>
    /// If true, this marking bypasses standard marking point limits and cannot be removed by the player.
    /// </summary>
    public bool Forced;

    /// <summary>
    /// Initializes a new instance with specified properties.
    /// </summary>
    public Marking(bool overrideAppearance, Enum layer, ProtoId<MarkingCategoryPrototype> category, ProtoId<MarkingPrototype> id, SpriteSpecifier sprite, string index, Color? color = null)
    {
        OverrideAppearance = overrideAppearance;
        Color = color ?? Color.White;
        Layer = layer;
        Category = category;
        Id = id;
        Sprite = sprite;
        Index = index;
    }

    /// <summary>
    /// Determines whether this marking is structurally equal to another.
    /// </summary>
    public bool Equals(Marking other)
    {
        if (!OverrideAppearance.Equals(other.OverrideAppearance))
            return false;

        if (!Color.Equals(other.Color))
            return false;

        if (!Layer.Equals(other.Layer))
            return false;

        if (!Category.Equals(other.Category))
            return false;

        if (!Id.Equals(other.Id))
            return false;

        if (!Sprite.Equals(other.Sprite))
            return false;

        if (!Index.Equals(other.Index))
            return false;

        if (!Forced.Equals(other.Forced))
            return false;

        return true;
    }

    /// <summary>
    /// Returns a new marking with the new color.
    /// </summary>
    /// <param name="color">The color to use for the marking.</param>
    /// <returns>A new marking with the specified color.</returns>
    public Marking WithColor(Color color)
    {
        return this with { Color = color };
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Color, Layer, Category, Id, Sprite, Index, Forced);
    }
}

