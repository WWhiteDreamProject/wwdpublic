using Robust.Shared.Prototypes;

namespace Content.Shared._NC.Cyberware;

[Prototype("therapyEmote")]
public sealed class TherapyEmotePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("lines", required: true)]
    public List<string> Lines { get; private set; } = new();
}
