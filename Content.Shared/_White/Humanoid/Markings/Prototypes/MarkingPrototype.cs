using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._White.Humanoid.Markings.Prototypes;

[Prototype]
public sealed partial class MarkingPrototype : IPrototype
{
    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Do we need to apply any displacement maps to this marking? Set to false if your marking is incompatible
    /// with a standard human doll, and is used for some special races with unusual shapes
    /// </summary>
    [DataField]
    public bool CanBeDisplaced { get; private set; } = true;

    /// <summary>
    /// If true, the color palette of this marking will be forced, ignoring customization options.
    /// </summary>
    [DataField]
    public bool ForcedColoring { get; private set; }

    /// <summary>
    /// The core data defining the appearance of the marking (layers, textures).
    /// </summary>
    [DataField(required: true)]
    public List<MarkingData> Markings { get; private set; } = new();

    /// <summary>
    /// An optional whitelist of marking groups that this specific marking can belong to.
    /// If null, it can belong to any group.
    /// </summary>
    [DataField]
    public List<ProtoId<MarkingGroupPrototype>>? GroupWhitelist { get; private set; }

    /// <summary>
    /// The default or preferred color settings for this marking.
    /// </summary>
    [DataField]
    public MarkingColors Coloring { get; private set; } = new();

    /// <summary>
    /// The category this marking belongs to (e.g., Torso, Left Hand, Overlay).
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MarkingCategoryPrototype> Category { get; private set; }

    /// <summary>
    /// Restricts which sex this marking can be applied to. If null, applies to all sexes.
    /// </summary>
    [DataField]
    public Sex? SexRestriction { get; private set; }
}

[DataDefinition]
public sealed partial class MarkingData
{
    [DataField(required: true)]
    public Enum Layer { get; private set; }

    [DataField(required: true)]
    public SpriteSpecifier Sprite { get; private set; }
}
