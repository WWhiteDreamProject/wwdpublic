using Robust.Shared.Prototypes;

namespace Content.Shared._NC.Cyberware;

[Prototype("therapyWordPool")]
public sealed class TherapyWordPoolPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("words", required: true)]
    public List<string> Words { get; private set; } = new();
}
