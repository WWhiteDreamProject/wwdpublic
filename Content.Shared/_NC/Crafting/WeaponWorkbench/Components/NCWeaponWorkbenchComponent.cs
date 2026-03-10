using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared._NC.Crafting.WeaponWorkbench.Prototypes;

namespace Content.Shared._NC.Crafting.WeaponWorkbench.Components;

/// <summary>
/// Состояния станка/верстака для крафта оружия.
/// </summary>
[Serializable, NetSerializable]
public enum NCWeaponWorkbenchState : byte
{
    Idle,       // Ожидание ввода материалов
    Processing, // Идёт мини-игра калибровки
    Success,    // Мини-игра пройдена, деталь готова
    Failed      // Фатальная ошибка, получен мусор
}

[Serializable, NetSerializable]
public enum NCWeaponWorkbenchVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum NCWeaponWorkbenchVisualLayers : byte
{
    Base
}

/// <summary>
/// Компонент высокотехнологичного верстака (ЧПУ-станка), для которого
/// требуется мини-игра активной калибровки.
/// Рецепт не используется — станок берёт болванку и обрабатывает её.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NCWeaponWorkbenchComponent : Component
{
    /// <summary>
    /// Текущее состояние работы станка.
    /// </summary>
    [DataField("state"), AutoNetworkedField]
    public NCWeaponWorkbenchState State = NCWeaponWorkbenchState.Idle;

    /// <summary>
    /// ID контейнера для базовой болванки/материала.
    /// </summary>
    public const string MaterialSlotId = "workbench_material_slot";

    #region Данные мини-игры активной калибровки

    // Датчики нормализованы от 0.0f до 1.0f

    /// <summary>
    /// Температура: стремится к 1.0f (перегрев).
    /// </summary>
    [AutoNetworkedField]
    public float Heat = 0.5f;

    /// <summary>
    /// Целостность: стремится к 0.0f (растрескивание).
    /// </summary>
    [AutoNetworkedField]
    public float Integrity = 0.5f;

    /// <summary>
    /// Калибровка: дрожит в обе стороны.
    /// </summary>
    [AutoNetworkedField]
    public float Alignment = 0.5f;

    /// <summary>
    /// Прогресс вытачивания (от 0.0f до 1.0f). При 1.0 — успех.
    /// </summary>
    [AutoNetworkedField]
    public float Progress = 0f;

    /// <summary>
    /// Сообщение для UI (предупреждения / сбои).
    /// </summary>
    [AutoNetworkedField]
    public string WarningMessage = string.Empty;

    /// <summary>
    /// Таймер нахождения датчиков в красной зоне.
    /// При превышении ToleranceTime — фатальный провал.
    /// </summary>
    [AutoNetworkedField]
    public float CriticalTimer = 0f;

    /// <summary>
    /// Отработавшие аномалии (по ключу прогресса), чтобы не срабатывали повторно.
    /// </summary>
    [AutoNetworkedField]
    public HashSet<float> TriggeredAnomalies = new();

    /// <summary>
    /// Кулдаун кнопок оператора (0.5 сек по диздоку).
    /// </summary>
    [AutoNetworkedField]
    public float ButtonCooldownTimer = 0f;

    /// <summary>
    /// Ширина безопасной зоны (половина), задаётся в прототипе станка.
    /// Тир 1 = 0.25, Тир 2 = 0.15, Тир 3 = 0.075.
    /// </summary>
    [DataField("safeZoneHalf"), AutoNetworkedField]
    public float CurrentSafeZoneHalf = 0.25f;

    /// <summary>
    /// Таймер допуска в красной зоне (секунды). Задаётся в прототипе станка.
    /// Тир 1 = 3.0, Тир 2 = 1.5, Тир 3 = 1.0.
    /// </summary>
    [DataField("toleranceTime"), AutoNetworkedField]
    public float ToleranceTime = 3.0f;

    /// <summary>
    /// Скорость прогресса (единиц в секунду).
    /// </summary>
    [DataField("progressSpeed"), AutoNetworkedField]
    public float ProgressSpeed = 0.05f;

    /// <summary>
    /// Паттерн аномалий верстака: процент прогресса -> тип аномалии.
    /// Задаётся через прототип, жёстко детерминирован.
    /// </summary>
    [DataField("anomalies")]
    public Dictionary<float, NCWorkbenchAnomalyType> Anomalies = new();

    /// <summary>
    /// Прототип результата обработки болванки.
    /// </summary>
    [DataField("resultEntity")]
    public string? ResultEntityId;

    /// <summary>
    /// Таймер красного мигания экрана при срабатывании аномалии.
    /// </summary>
    [AutoNetworkedField]
    public float FlashTimer = 0f;

    /// <summary>
    /// "Окно безопасности" после аномалии (секунды), когда CriticalTimer не тикает.
    /// Чтобы игрок успел среагировать на резкий скачок.
    /// </summary>
    [AutoNetworkedField]
    public float AnomalyGraceTimer = 0f;

    /// <summary>
    /// Включена ли системная блокировка для этого верстака (Тир 3).
    /// </summary>
    [DataField("enableSystemLock")]
    public bool EnableSystemLock = false;

    /// <summary>
    /// Сейчас система заблокирована. Игрок должен ввести код.
    /// </summary>
    [AutoNetworkedField]
    public bool IsSystemLocked = false;

    /// <summary>
    /// Сгенерированный код допуска (4 цифры).
    /// </summary>
    [AutoNetworkedField]
    public string LockCode = string.Empty;

    /// <summary>
    /// Блокировка уже срабатывала в этом цикле.
    /// </summary>
    [AutoNetworkedField]
    public bool LockTriggered = false;

    #endregion
}
