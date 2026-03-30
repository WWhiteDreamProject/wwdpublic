using Robust.Shared.Prototypes;

namespace Content.Shared._NC.Cyberware;

/// <summary>
///     Глобальный пул слов для нейро-терапии (протокол Войт-Кампф).
/// </summary>
[Prototype("traumaPool")]
public sealed class TraumaPoolPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("words", required: true)]
    public List<string> Words { get; private set; } = new();
}
