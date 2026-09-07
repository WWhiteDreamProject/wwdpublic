using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Prototypes;

[Prototype]
public sealed class BodyTypePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Name of the body type.
    /// </summary>
    [DataField(required: true)]
    public string Name { get; } = default!;

    /// <summary>
    /// Which sex can't use this body type? TODO: In the bright future all restrictions should use refactored CharacterRequirement.
    /// </summary>
    [DataField]
    public List<Sex> SexRestrictions = new();
}
