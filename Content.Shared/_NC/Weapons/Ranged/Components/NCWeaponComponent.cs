using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Weapons.Ranged.NCWeapon;

/// <summary>
/// Производители оружия Найт-Сити, разделённые по тирам качества.
/// </summary>
[NetSerializable, Serializable]
public enum WeaponManufacturer : byte
{
    // ТИР-0: «Одноразовый пластик»
    GunMart,
    BudgetArms,
    DaiLung,
    Sanroo,
    DarraPolytechnic,

    // ТИР-1: «Рабочие лошадки»
    FederatedArms,
    ConstitutionArms,
    Militech,
    Rostovic,
    KangTao,
    ChadranArms,
    MustangArms,
    Techtronika,

    // ТИР-2: «Корпоративная Элита»
    Sternmeyer,
    Arasaka,
    TsunamiArms,
    Kendachi,
    CenturionEssentials,

    // ТИР-3: «Уникальный кастом»
    MalorianArms,
    Nomad
}

/// <summary>
/// Тир качества оружия — определяет общие правила по ремонту и надёжности.
/// </summary>
[NetSerializable, Serializable]
public enum WeaponTier : byte
{
    Poor,       // ТИР-0: нельзя починить
    Standard,   // ТИР-1: регулярная чистка, без смарт-систем
    Excellent,  // ТИР-2: идеальная эргономика, фирменные детали
    Legendary   // ТИР-3: нарушает стандарты, ремонт — искусство
}

/// <summary>
/// Компонент оружия Найт-Сити. Содержит параметры бренда, прочности,
/// механик деградации и состояния оружия.
/// Обрабатывается NCWeaponSystem на сервере.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NCWeaponComponent : Component
{
    #region Идентификация бренда

    /// <summary>
    /// Производитель оружия — определяет уникальную механику.
    /// </summary>
    [DataField("manufacturer"), AutoNetworkedField]
    public WeaponManufacturer Manufacturer = WeaponManufacturer.FederatedArms;

    /// <summary>
    /// Тир качества оружия.
    /// </summary>
    [DataField("tier"), AutoNetworkedField]
    public WeaponTier Tier = WeaponTier.Standard;



    #endregion

    #region Прочность и износ

    /// <summary>
    /// Текущая прочность оружия. При 0 — оружие ломается.
    /// </summary>
    [DataField("durability"), AutoNetworkedField]
    public float Durability = 100f;

    /// <summary>
    /// Максимальная прочность (для examine и расчётов).
    /// </summary>
    [DataField("maxDurability"), AutoNetworkedField]
    public float MaxDurability = 100f;

    /// <summary>
    /// Скорость износа за выстрел (вычитается из Durability).
    /// </summary>
    [DataField("degradationRate"), AutoNetworkedField]
    public float DegradationRate = 1f;

    /// <summary>
    /// Счётчик выстрелов с последнего обслуживания (для механик износа).
    /// </summary>
    [AutoNetworkedField]
    public int ShotsFired;

    /// <summary>
    /// Можно ли починить оружие. ТИР-0 обычно false.
    /// </summary>
    [DataField("isRepairable"), AutoNetworkedField]
    public bool IsRepairable = true;

    #endregion

    #region Заклинивание

    /// <summary>
    /// Базовый шанс заклинивания за выстрел (0.0–1.0).
    /// Увеличивается при износе.
    /// </summary>
    [DataField("jamChance"), AutoNetworkedField]
    public float JamChance;

    /// <summary>
    /// Текущее состояние — заклинило ли оружие.
    /// </summary>
    [AutoNetworkedField]
    public bool IsJammed;

    /// <summary>
    /// Время (в секундах), необходимое для извлечения заклинившего патрона.
    /// </summary>
    [DataField("unjamTime"), AutoNetworkedField]
    public float UnjamTime = 2f;

    #endregion

    #region Перегрев

    /// <summary>
    /// Количество тепла, генерируемого за выстрел.
    /// 0 = оружие не перегревается.
    /// </summary>
    [DataField("overheatHeat"), AutoNetworkedField]
    public float OverheatHeat;

    /// <summary>
    /// Порог перегрева — при превышении наносится урон стрелку.
    /// </summary>
    [DataField("overheatThreshold"), AutoNetworkedField]
    public float OverheatThreshold = 100f;

    /// <summary>
    /// Текущий уровень тепла.
    /// </summary>
    [AutoNetworkedField]
    public float CurrentHeat;

    /// <summary>
    /// Скорость остывания (единиц тепла в секунду).
    /// </summary>
    [DataField("cooldownRate"), AutoNetworkedField]
    public float CooldownRate = 5f;

    /// <summary>
    /// Оружие перегрето — наносит урон стрелку при стрельбе.
    /// </summary>
    [AutoNetworkedField]
    public bool IsOverheated;

    #endregion

    #region Катастрофические отказы

    /// <summary>
    /// Шанс взрыва оружия в руках при каждом выстреле (0.0–1.0).
    /// Для BudgetArms увеличивается при низкой прочности.
    /// </summary>
    [DataField("explodeChance"), AutoNetworkedField]
    public float ExplodeChance;

    /// <summary>
    /// Шанс безвозвратной поломки при выстреле (0.0–1.0).
    /// Для DaiLung — при промахе.
    /// </summary>
    [DataField("breakChance"), AutoNetworkedField]
    public float BreakChance;

    /// <summary>
    /// Оружие сломано — нельзя стрелять.
    /// </summary>
    [AutoNetworkedField]
    public bool IsBroken;

    /// <summary>
    /// Урон, наносимый стрелку при взрыве оружия.
    /// </summary>
    [DataField("explodeDamage")]
    public DamageSpecifier? ExplodeDamage;

    /// <summary>
    /// Звук взрыва оружия (BudgetArms и др.).
    /// </summary>
    [DataField("explodeSound")]
    public Robust.Shared.Audio.SoundSpecifier? ExplodeSound;

    /// <summary>
    /// Звук поломки оружия (DaiLung и др.).
    /// </summary>
    [DataField("breakSound")]
    public Robust.Shared.Audio.SoundSpecifier? BreakSound;

    /// <summary>
    /// Урон, наносимый стрелку при перегреве (термический).
    /// </summary>
    [DataField("overheatDamage")]
    public DamageSpecifier? OverheatDamage;

    /// <summary>
    /// Звук перегрева оружия (DarraPolytechnic и др.).
    /// </summary>
    [DataField("overheatSound")]
    public Robust.Shared.Audio.SoundSpecifier? OverheatSound;

    #endregion

    #region Модификаторы боя

    /// <summary>
    /// Требуется ли смарт-линк для полного функционала.
    /// </summary>
    [DataField("requiresSmartLink"), AutoNetworkedField]
    public bool RequiresSmartLink;

    /// <summary>
    /// Минимальный навык для использования без штрафов.
    /// </summary>
    [DataField("minSkillToUse"), AutoNetworkedField]
    public float MinSkillToUse;

    /// <summary>
    /// Штраф к урону стрелка от отдачи (без кибер-рук).
    /// </summary>
    [DataField("recoilDamagePenalty"), AutoNetworkedField]
    public float RecoilDamagePenalty;

    /// <summary>
    /// Бонус к урону в ближнем бою (прикладом).
    /// </summary>
    [DataField("meleeDamageBonus"), AutoNetworkedField]
    public float MeleeDamageBonus;

    /// <summary>
    /// Множитель времени доставания из кобуры.
    /// > 1.0 = медленнее, < 1.0 = быстрее.
    /// </summary>
    [DataField("drawTimeMultiplier"), AutoNetworkedField]
    public float DrawTimeMultiplier = 1f;

    /// <summary>
    /// Множитель времени перезарядки.
    /// > 1.0 = медленнее, < 1.0 = быстрее.
    /// </summary>
    [DataField("reloadTimeMultiplier"), AutoNetworkedField]
    public float ReloadTimeMultiplier = 1f;

    /// <summary>
    /// Множитель разброса (AngleIncrease). Для Sanroo = 2.5.
    /// </summary>
    [DataField("spreadMultiplier"), AutoNetworkedField]
    public float SpreadMultiplier = 1f;

    /// <summary>
    /// Множитель урона снарядов. Для Sanroo = 0.5.
    /// </summary>
    [DataField("damageMultiplier"), AutoNetworkedField]
    public float DamageMultiplier = 1f;

    #endregion
}
