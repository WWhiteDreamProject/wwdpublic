using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._White.Humanoid.Markings.Prototypes;

/// <summary>
/// Marker prototype that defines well-known types of markings, e.g. "human", "NT prosthetic", "moth", etc.
/// </summary>
[Prototype]
public sealed partial class MarkingsGroupPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<MarkingsGroupPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance, AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// If only markings that explicitly list the group of this organ are permitted
    /// </summary>
    [DataField]
    public bool OnlyGroupWhitelisted;

    [DataField, AlwaysPushInheritance]
    public Dictionary<ProtoId<MarkingCategoryPrototype>, MarkingsLimits> Limits = new();

    [DataField, AlwaysPushInheritance]
    public Dictionary<ProtoId<MarkingCategoryPrototype>, MarkingsAppearance> Appearances = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class MarkingsLimits
{
    /// <summary>
    /// How many markings this layer can take
    /// </summary>
    [DataField(required: true)]
    public int Limit;

    /// <summary>
    /// Whether or not this layer is required to have a marking
    /// </summary>
    [DataField(required: true)]
    public bool Required;

    /// <summary>
    /// If only markings that explicitly list the group of this organ are permitted
    /// </summary>
    [DataField]
    public bool? OnlyGroupWhitelisted;

    /// <summary>
    /// Default markings for this layer.
    /// </summary>
    [DataField]
    public List<ProtoId<MarkingPrototype>> Default = new();

    /// <summary>
    /// Nudity markings for this layer that will be ensured if it is being enforced.
    /// </summary>
    [DataField]
    public List<ProtoId<MarkingPrototype>> NudityDefault = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class MarkingsAppearance
{
    /// <summary>
    /// The transparency that marking has.
    /// </summary>
    [DataField]
    public float LayerAlpha = 1f;

    /// <summary>
    /// Whether markings should be forced to match the skin color.
    /// </summary>
    [DataField]
    public bool MatchSkin;
}
