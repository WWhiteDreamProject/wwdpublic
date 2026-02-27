using Content.Shared._White.Humanoid.SkinColoration;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Prototypes;

/// <summary>
/// A prototype containing a SkinColorationStrategy
/// </summary>
[Prototype]
public sealed partial class SkinColorationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The skin coloration strategy specified by this prototype
    /// </summary>
    [DataField(required: true)]
    public ISkinColorationStrategy Strategy = default!;
}
