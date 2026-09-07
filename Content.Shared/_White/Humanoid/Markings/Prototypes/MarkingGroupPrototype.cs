using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._White.Humanoid.Markings.Prototypes;

/// <summary>
/// Defines a collection of marking constraints and defaults for specific humanoid species or body types.
/// </summary>
[Prototype]
public sealed partial class MarkingGroupPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [IdDataField]
    public string ID { get; } = default!;

    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<MarkingGroupPrototype>))]
    public string[]? Parents { get; }

    /// <inheritdoc />
    [NeverPushInheritance, AbstractDataField]
    public bool Abstract { get; }

    /// <summary>
    /// If true, only markings that explicitly include this group in their whitelist will be selectable.
    /// </summary>
    [DataField]
    public bool OnlyGroupWhitelisted { get; }

    /// <summary>
    /// Defines constraints for specific marking categories (e.g., how many tattoos can be applied).
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public Dictionary<ProtoId<MarkingCategoryPrototype>, MarkingCategoryData> CategoriesData { get; } = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class MarkingCategoryData
{
    /// <summary>
    /// If true, at least one marking must be selected for a specific category.
    /// </summary>
    [DataField]
    public bool Required;

    /// <summary>
    /// If set, overrides the group-level whitelist check for a specific category.
    /// </summary>
    [DataField]
    public bool? OnlyGroupWhitelisted;

    /// <summary>
    /// The transparency multiplier applied to a specific category.
    /// </summary>
    [DataField]
    public float LayerAlpha = 1f;

    /// <summary>
    /// A set of markings applied by default.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<MarkingPrototype>> Default = new();

    /// <summary>
    /// A set of markings applied automatically if it is being enforced.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<MarkingPrototype>> Nudity = new();

    /// <summary>
    /// The maximum number of markings allowed for a specific category.
    /// </summary>
    [DataField(required: true)]
    public int Limit;

    /// <summary>
    /// If set, forces the marking color to sync with the coloring definition.
    /// </summary>
    [DataField]
    public MarkingColoringDefinition? Coloring;
}
