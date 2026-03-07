using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Crafting.WeaponAssembly;

/// <summary>
/// Компонент обработанной детали оружия — ствол, рамка, плата.
/// Определяет тип детали и с чем она может комбинироваться.
/// Ставится на выходные сущности верстака (этап 2).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NCWeaponPartComponent : Component
{
    /// <summary>
    /// Тип детали (какая именно деталь).
    /// </summary>
    [DataField("partType", required: true), AutoNetworkedField]
    public NCWeaponPartType PartType = NCWeaponPartType.Barrel;

    /// <summary>
    /// Какой тип детали нужен СЛЕДУЮЩИМ для продолжения сборки.
    /// Если null — деталь финальная (дальше нечего присоединять).
    /// </summary>
    [DataField("acceptsNext"), AutoNetworkedField]
    public NCWeaponPartType? AcceptsNext;

    /// <summary>
    /// Какая сущность создаётся при успешной комбинации с нужной деталью.
    /// </summary>
    [DataField("combineResult"), AutoNetworkedField]
    public string? CombineResultId;
}

/// <summary>
/// Типы оружейных деталей.
/// </summary>
[Serializable, NetSerializable]
public enum NCWeaponPartType : byte
{
    Barrel,      // Обработанный ствол
    Frame,       // Полимерная рамка / ствольная коробка
    Board,       // Печатная плата / электроника
    BarrelFrame, // Промежуточная сборка: ствол + рамка
    Weapon       // Финальное оружие (больше нельзя комбинировать)
}
