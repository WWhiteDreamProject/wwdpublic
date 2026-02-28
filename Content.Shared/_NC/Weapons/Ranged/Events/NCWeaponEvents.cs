// Кастомные события системы оружейных брендов Найт-Сити
// Путь: Content.Shared/_NC/Weapons/Ranged/Events/NCWeaponEvents.cs

namespace Content.Shared._NC.Weapons.Ranged.Events;

/// <summary>
/// Рейзится на оружии, когда оно заклинивает (зажёвывает патрон).
/// Бренды: GunMart, FederatedArms (редко).
/// </summary>
[ByRefEvent]
public record struct WeaponJammedEvent(EntityUid User, EntityUid Weapon);

/// <summary>
/// Рейзится на оружии при перегреве от интенсивной стрельбы.
/// Бренды: DarraPolytechnic.
/// </summary>
[ByRefEvent]
public record struct WeaponOverheatEvent(EntityUid User, EntityUid Weapon, float CurrentHeat);

/// <summary>
/// Рейзится на оружии при катастрофической поломке (безвозвратное уничтожение).
/// Бренды: DaiLung.
/// </summary>
[ByRefEvent]
public record struct WeaponBrokeEvent(EntityUid User, EntityUid Weapon);

/// <summary>
/// Рейзится на оружии при взрыве в руках стрелка.
/// Бренды: BudgetArms.
/// </summary>
[ByRefEvent]
public record struct WeaponExplodedEvent(EntityUid User, EntityUid Weapon);

/// <summary>
/// Рейзится на оружии при деградации прочности после выстрела.
/// Используется для прогрессивного износа.
/// </summary>
[ByRefEvent]
public record struct WeaponDegradedEvent(EntityUid Weapon, float OldDurability, float NewDurability);
