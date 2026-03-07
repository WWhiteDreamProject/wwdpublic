using Content.Shared._NC.Weapons.Ranged.NCWeapon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Crafting.WeaponWorkbench.Prototypes;

/// <summary>
/// Типы аномалий, возникающих при жестко закодированном сбое.
/// </summary>
[Serializable, NetSerializable]
public enum NCWorkbenchAnomalyType : byte
{
    HeatSpike,      // Резкий рост Температуры
    IntegrityDrop,  // Резкое падение Целостности
    AlignmentLeft,  // Резкий сбой Калибровки влево
    AlignmentRight, // Резкий сбой Калибровки вправо
    DoubleTrouble   // Двойной сбой (Температура и Целостность)
}

/// <summary>
/// Прототип рецепта для ЧПУ-станка.
/// Определяет, что нужно загрузить, что на выходе, и какие паттерны аномалий.
/// </summary>
[Prototype("ncWeaponRecipe")]
public sealed class NCWeaponRecipePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Тир рецепта (влияет на сложность).
    /// </summary>
    [DataField("tier")]
    public WeaponTier Tier = WeaponTier.Poor;

    /// <summary>
    /// ID прототипа сущности, которая требуется на входе (например, "NCBlankBarrelPolymer").
    /// </summary>
    [DataField("baseMaterialId", required: true)]
    public string BaseMaterialId = string.Empty;

    /// <summary>
    /// ID прототипа сущности, которая будет создана при 100% успехе.
    /// </summary>
    [DataField("resultEntityId", required: true)]
    public string ResultEntityId = string.Empty;

    /// <summary>
    /// Множитель скорости прогресса (меньше = дольше идет 0-100%).
    /// </summary>
    [DataField("progressSpeed")]
    public float ProgressSpeed = 0.05f; // 20 секунд по умолчанию

    /// <summary>
    /// Закодированные аномалии: процент прогресса (0.0 - 1.0) -> Тип аномалии.
    /// </summary>
    [DataField("anomalies")]
    public Dictionary<float, NCWorkbenchAnomalyType> Anomalies = new();
}
