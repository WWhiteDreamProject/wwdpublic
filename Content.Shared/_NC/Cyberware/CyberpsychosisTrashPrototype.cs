using Robust.Shared.Prototypes;

namespace Content.Shared._NC.Cyberware;

/// <summary>
///     Прототип для хранения списка фраз "словесного мусора" при киберпсихозе.
/// </summary>
[Prototype("cyberpsychosisTrash")]
public sealed class CyberpsychosisTrashPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("trash", required: true)]
    public List<string> Trash { get; private set; } = new();
}