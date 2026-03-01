// Стукач Томми — компонент для двойного магазина (пистолет + дробовик)
// Путь: Content.Shared/_NC/Weapons/Ranged/DualAmmo/DualAmmoProviderComponent.cs

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared._NC.Weapons.Ranged.DualAmmo;

/// <summary>
/// Компонент для оружия с двумя независимыми магазинами.
/// Позволяет переключаться между режимами стрельбы (например, пистолет/дробовик).
/// Каждый режим имеет свой тип боеприпасов и отдельный счётчик патронов.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DualAmmoProviderComponent : Component
{
    /// <summary>
    /// Текущий активный режим огня (индекс в списке Modes).
    /// 0 = первый режим (обычно пистолет), 1 = второй режим (обычно дробовик).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentMode = 0;

    /// <summary>
    /// Список доступных режимов огня с их характеристиками.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<DualAmmoMode> Modes = new();
}

/// <summary>
/// Определение одного режима огня для DualAmmoProvider.
/// Содержит информацию о типе снаряда, ёмкости магазина и текущем количестве патронов.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class DualAmmoMode
{
    /// <summary>
    /// Использует ли этот режим съемные магазины (MagazineAmmoProvider).
    /// Если true, то Prototype, Capacity и Count игнорируются, а патроны берутся из ItemSlot "gun_magazine".
    /// </summary>
    [DataField]
    public bool UsesMagazine = false;

    /// <summary>
    /// Прототип снаряда для данного режима.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype = default!;

    /// <summary>
    /// Максимальная ёмкость магазина для данного режима.
    /// </summary>
    [DataField]
    public int Capacity = 8;

    /// <summary>
    /// Текущее количество патронов в магазине.
    /// </summary>
    [DataField]
    public int Count = 0;

    /// <summary>
    /// Локализованное название режима (для UI и popup).
    /// </summary>
    [DataField]
    public LocId ModeName = "dual-ammo-mode-default";

    /// <summary>
    /// Звук выстрела для данного режима (опционально).
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundGunshot;

    /// <summary>
    /// Скорострельность для данного режима (выстрелов в секунду).
    /// </summary>
    [DataField]
    public float? FireRate;

    /// <summary>
    /// Whitelist тегов для принимаемых боеприпасов.
    /// </summary>
    [DataField]
    public List<string>? WhitelistTags;
}
