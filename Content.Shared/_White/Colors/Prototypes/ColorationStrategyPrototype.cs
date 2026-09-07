using Content.Shared._White.Colors.Strategies;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Colors.Prototypes;

/// <summary>
/// A prototype containing a coloration strategy.
/// </summary>
[Prototype]
public sealed partial class ColorationStrategyPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The coloration strategy specified by this prototype.
    /// </summary>
    [DataField(required: true)]
    public IColorationStrategy Strategy = default!;
}
