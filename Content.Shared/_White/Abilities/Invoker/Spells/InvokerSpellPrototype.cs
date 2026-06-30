using Robust.Shared.Prototypes;

namespace Content.Shared._White.Abilities.Invoker;

[Prototype("invokerSpell")]
public sealed partial class InvokerSpellPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public List<OrbType> Combination = new();

    [DataField(required: true)]
    public EntProtoId Action = default!;

    [DataField]
    public ComponentRegistry Components = new();
}

[Prototype("invokerSpellPool")]
public sealed partial class InvokerSpellPoolPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField]
    public List<string> Spells = new();
}
