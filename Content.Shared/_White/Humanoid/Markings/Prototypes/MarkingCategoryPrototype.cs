using Content.Shared._White.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Markings.Prototypes;

/// <summary>
/// Marker prototype that defines well-known types of marking category, e.g. "left arm" or "torso".
/// </summary>
[Prototype]
public sealed partial class MarkingCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The type of body provider to which the marking category is attached.
    /// </summary>
    [DataField]
    public BodyProviderType Type { get; }
}
