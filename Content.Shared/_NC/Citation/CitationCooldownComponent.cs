using Robust.Shared.Serialization;

namespace Content.Shared._NC.Citation;

/// <summary>
/// Компонент, добавляемый игроку, чтобы отслеживать кулдаун штрафов.
/// Защита от спама/гриферства со стороны копов.
/// </summary>
[RegisterComponent]
public sealed partial class CitationCooldownComponent : Component
{
    /// <summary>
    /// Словарь, хранящий время последнего штрафа от конкретного ID копа (или просто глобальное время последнего штрафа).
    /// Сделаем глобальный кулдаун, чтобы не спамили другие копы тоже.
    /// </summary>
    [DataField("lastCitationTime")]
    public TimeSpan LastCitationTime;

    /// <summary>
    /// Длительность кулдауна в секундах (5-10 минут по ТЗ = 300-600 секунд).
    /// </summary>
    [DataField("cooldownDuration")]
    public TimeSpan CooldownDuration = TimeSpan.FromMinutes(5);
}