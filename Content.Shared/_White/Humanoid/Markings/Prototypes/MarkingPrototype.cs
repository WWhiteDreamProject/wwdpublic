using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._White.Humanoid.Markings.Prototypes;

/// <summary>
/// Defines a specific marking (tattoo, scar, etc.) that can be applied to a humanoid entity.
/// </summary>
[Prototype]
public sealed partial class MarkingPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [IdDataField]
    public string ID { get; } = default!;

    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<MarkingPrototype>))]
    public string[]? Parents { get; }

    /// <inheritdoc />
    [NeverPushInheritance, AbstractDataField]
    public bool Abstract { get; }

    /// <summary>
    /// Whether this marking should be subject to any displacement maps.
    /// </summary>
    [DataField]
    public bool CanBeDisplaced { get; } = true;

    /// <summary>
    /// If true, the color palette of this marking will be forced, ignoring customization options.
    /// </summary>
    [DataField]
    public bool ForcedColoring { get; }

    /// <summary>
    /// An optional whitelist of marking groups that this specific marking can belong to.
    /// If null, it can belong to any group.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<MarkingGroupPrototype>>? Groups { get; }

    /// <summary>
    /// A collection of layers and their associated textures that compose this marking.
    /// </summary>
    [DataField(required: true)]
    public List<MarkingDefinition> Definitions { get; } = new();

    /// <summary>
    /// The default color configuration for this marking.
    /// </summary>
    [DataField]
    public MarkingColors Coloring { get; } = new();

    /// <summary>
    /// The category this marking belongs to (e.g., Torso, Left Hand, Overlay).
    /// </summary>
    [DataField]
    public ProtoId<MarkingCategoryPrototype> Category { get; } = "None";

    /// <summary>
    /// Restricts which sex this marking can be applied to. If null, applies to all sexes. TODO: In the bright future all restrictions should use refactored CharacterRequirement.
    /// </summary>
    [DataField]
    public Sex? SexRestriction { get; }
}

/// <summary>
/// Defines appearance data for marking.
/// </summary>
[DataDefinition]
public sealed partial class MarkingDefinition
{
    /// <summary>
    /// Determines whether this marker overrides the appearance provider's sprite.
    /// </summary>
    [DataField]
    public bool OverrideAppearance;

    /// <summary>
    /// The layer associated with this marking data.
    /// </summary>
    [DataField(required: true)]
    public Enum Layer;

    /// <summary>
    /// The sprite associated with this marking data.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Sprite;

    /// <summary>
    /// The index associated with this marking data.
    /// </summary>
    [DataField]
    public string Index = "default";
}

/// <summary>
/// Defines default color profiles for marking.
/// </summary>
[DataDefinition]
public sealed partial class MarkingColors
{
    /// <summary>
    /// The default color properties applied to layers that do not have specific overrides.
    /// </summary>
    [DataField(readOnly:true)]
    public MarkingColoringDefinition Default = new();

    /// <summary>
    /// A map of specific index to their unique coloring properties.
    /// </summary>
    [DataField(readOnly:true)]
    public Dictionary<string, MarkingColoringDefinition>? Indexes;
}
