// Стукач Томми — система дропа оружия при стрельбе без wield
// Путь: Content.Shared/_NC/Weapons/Ranged/DropOnShoot/DropOnShootSystem.cs

using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Timing;

namespace Content.Shared._NC.Weapons.Ranged.DropOnShoot;

/// <summary>
/// Система обработки дропа оружия при стрельбе без удержания двумя руками.
/// При выстреле проверяет, находится ли оружие в режиме wield.
/// Если нет — оружие выпадает из рук игрока.
/// </summary>
public sealed class DropOnShootSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Подписываемся на событие выстрела
        SubscribeLocalEvent<DropOnShootComponent, GunShotEvent>(OnGunShot);
    }

    /// <summary>
    /// Обработка выстрела — проверка wield и дроп при необходимости.
    /// </summary>
    private void OnGunShot(EntityUid uid, DropOnShootComponent component, ref GunShotEvent args)
    {
        // Проверяем, есть ли компонент Wieldable и находится ли оружие в режиме wield
        if (TryComp<WieldableComponent>(uid, out var wieldable) && wieldable.Wielded)
            return; // Оружие удерживается двумя руками — всё в порядке

        if (!_timing.IsFirstTimePredicted)
            return;

        // Оружие НЕ в режиме wield — дропаем его
        // Показываем popup игроку
        _popup.PopupClient(Loc.GetString(component.DropMessage), uid, args.User, PopupType.MediumCaution);

        // Дропаем оружие из рук
        _hands.TryDrop(args.User, uid, checkActionBlocker: false, doDropInteraction: false);
    }
}
