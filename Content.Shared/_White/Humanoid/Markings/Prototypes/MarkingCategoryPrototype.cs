using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Markings.Prototypes;

/// <summary>
/// Marker prototype that defines well-known types of marking category, e.g. "left arm" or "torso".
/// </summary>
[Prototype]
public sealed partial class MarkingCategoryPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;
}
