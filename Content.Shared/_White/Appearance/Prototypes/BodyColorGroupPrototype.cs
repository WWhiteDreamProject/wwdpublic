using Robust.Shared.Prototypes;

namespace Content.Shared._White.Appearance.Prototypes;

[Prototype]
public sealed partial class BodyColorGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
