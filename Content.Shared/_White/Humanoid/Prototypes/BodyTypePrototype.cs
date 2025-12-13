using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Prototypes;

[Prototype("bodyType")]
public sealed class BodyTypePrototype : IPrototype
{
    /// <summary>
    ///     Prototype ID of the body type.
    /// </summary>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    ///     User visible name of the body type.
    /// </summary>
    [DataField(required: true)]
    public string Name { get; } = default!;

    /// <summary>
    ///     Which sex can't use this body type.
    /// </summary>
    [DataField]
    public List<string> SexRestrictions = new();
}
