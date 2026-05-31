using Content.Shared._White.Humanoid.Coloration;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Prototypes;

/// <summary>
/// A prototype containing a coloration strategy.
/// </summary>
[Prototype]
public sealed partial class ColorationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The coloration strategy specified by this prototype.
    /// </summary>
    [DataField(required: true)]
    public IColorationStrategy Strategy = default!;
}
