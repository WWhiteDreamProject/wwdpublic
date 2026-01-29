// Стукач Томми — компонент для дропа оружия при стрельбе без wield
// Путь: Content.Shared/_NC/Weapons/Ranged/DropOnShoot/DropOnShootComponent.cs

using Robust.Shared.GameStates;

namespace Content.Shared._NC.Weapons.Ranged.DropOnShoot;

/// <summary>
/// Компонент-маркер для оружия, которое выпадает из рук при стрельбе без удержания двумя руками (wield).
/// Используется для реализации механики "Стукач Томми" — комбинированного оружия с высокой отдачей.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DropOnShootComponent : Component
{
    /// <summary>
    /// Сообщение, отображаемое игроку при дропе оружия.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId DropMessage = "gun-drop-on-shoot";
}
