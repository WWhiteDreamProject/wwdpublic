using Content.Shared._NC.Crafting.WeaponWorkbench.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared._NC.Crafting.WeaponWorkbench.Components;

/// <summary>
/// Компонент необработанной детали (болванки).
/// Определяет сложность мини-игры на ЧПУ-станке и результат работы.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NCWeaponBlankComponent : Component
{
    /// <summary>
    /// Ширина безопасной зоны (половина). Тир 1 = 0.25, Тир 2 = 0.15, Тир 3 = 0.075.
    /// </summary>
    [DataField("safeZoneHalf")]
    public float SafeZoneHalf = 0.25f;

    /// <summary>
    /// Таймер допуска в красной зоне (секунды). Тир 1 = 3.0, Тир 2 = 1.5, Тир 3 = 1.0.
    /// </summary>
    [DataField("toleranceTime")]
    public float ToleranceTime = 3.0f;

    /// <summary>
    /// Скорость прогресса (единиц в секунду).
    /// </summary>
    [DataField("progressSpeed")]
    public float ProgressSpeed = 0.05f;

    /// <summary>
    /// Включена ли системная блокировка для этой болванки (Тир 3).
    /// </summary>
    [DataField("enableSystemLock")]
    public bool EnableSystemLock = false;

    /// <summary>
    /// Паттерн аномалий: процент прогресса -> тип аномалии.
    /// </summary>
    [DataField("anomalies")]
    public Dictionary<float, NCWorkbenchAnomalyType> Anomalies = new();

    /// <summary>
    /// Прототип результата (какая деталь получится при успехе).
    /// </summary>
    [DataField("resultEntity")]
    public string? ResultEntityId;
}
