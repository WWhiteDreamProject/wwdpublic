using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Markings.Prototypes;

/// <summary>
/// Defines a specific category for markings, such as "left arm," "torso," or "face."
/// </summary>
[Prototype]
public sealed partial class MarkingCategoryPrototype : IPrototype
{
    /// <inheritdoc />
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The specific layers that this category is responsible for visualizing.
    /// </summary>
    [DataField(required: true)]
    public HashSet<Enum> Layers = new();
}
