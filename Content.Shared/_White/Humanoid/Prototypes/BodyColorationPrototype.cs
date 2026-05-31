using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Prototypes;

[Prototype]
public sealed partial class BodyColorationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
