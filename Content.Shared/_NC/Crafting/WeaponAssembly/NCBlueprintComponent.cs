using Content.Shared.Tools;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Shared._NC.Crafting.WeaponAssembly;

[DataDefinition]
public sealed partial class NCAssemblyStep
{
    /// <summary>
    /// Если задано, на этом шаге игрок должен вставить указанную деталь.
    /// </summary>
    [DataField("part")]
    public NCWeaponPartType? Part;

    /// <summary>
    /// Если задано, на этом шаге игрок должен использовать инструмент с указанным качеством.
    /// Например: "Screwing", "Pulsing", "Welding".
    /// </summary>
    [DataField("tool", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string? ToolQuality;

    /// <summary>
    /// Если задано, на этом шаге игрок должен вставить ПРИСТРОГО указанную сущность (прототип).
    /// </summary>
    [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? RequiredPrototype;

    /// <summary>
    /// Время (в секундах) на сборку / установку.
    /// </summary>
    [DataField("doAfterTime")]
    public float DoAfterTime = 2.0f;

    /// <summary>
    /// Звук, который будет проигрываться во время DoAfter.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound;
}

/// <summary>
/// Компонент чертежа оружия. Используется как основа для Этапа 3.
/// Игрок поэтапно вставляет в него детали или применяет инструменты в строго заданном порядке.
/// Ошибка порядка ведёт к уничтожению чертежа и деталей.
/// </summary>
[RegisterComponent]
public sealed partial class NCBlueprintComponent : Component
{
    /// <summary>
    /// Массив строго заданных шагов сборки.
    /// </summary>
    [DataField("steps", required: true)]
    public List<NCAssemblyStep> Steps = new();

    /// <summary>
    /// Текущий шаг сборки (по индексу массива Steps).
    /// </summary>
    [DataField("currentStep")]
    public int CurrentStep = 0;

    /// <summary>
    /// Сущность, которая появится после успешного завершения всех шагов.
    /// </summary>
    [DataField("resultEntity", required: true)]
    public string ResultEntityId = string.Empty;

    /// <summary>
    /// Сущность (мусор), которая появится при ошибке сборки.
    /// </summary>
    [DataField("garbageEntity")]
    public string GarbageEntityId = "Wrench"; // Заглушка, желательно заменить на "Scrap"
}
