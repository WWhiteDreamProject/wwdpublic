using Content.Shared._NC.Crafting.WeaponAssembly;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Server._NC.Crafting.WeaponAssembly;

/// <summary>
/// Серверная система финальной сборки оружия в руках (Этап 3).
/// Игрок кликает одной деталью по другой. Если типы совпадают
/// по цепочке (AcceptsNext == PartType второй детали), обе детали
/// уничтожаются и спавнится результат. При несовпадении — фатальная
/// ошибка: обе детали уничтожаются и спавнится мусор.
/// </summary>
public sealed class NCWeaponAssemblySystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Перехватываем клик деталью по другой детали
        SubscribeLocalEvent<NCWeaponPartComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid targetUid, NCWeaponPartComponent targetPart, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Проверяем, что предмет в руке тоже является деталью
        if (!TryComp<NCWeaponPartComponent>(args.Used, out var usedPart))
            return;

        args.Handled = true;

        // Проверяем цепочку сборки:
        // targetPart.AcceptsNext должен совпадать с usedPart.PartType
        if (targetPart.AcceptsNext != null && targetPart.AcceptsNext == usedPart.PartType)
        {
            // Успешная комбинация
            SucceedAssembly(targetUid, targetPart, args.Used, args.User);
            return;
        }

        // Проверяем обратную комбинацию (если игрок кликнул в обратном порядке)
        if (usedPart.AcceptsNext != null && usedPart.AcceptsNext == targetPart.PartType)
        {
            // Успешная комбинация (обратный порядок — берём результат из usedPart)
            SucceedAssembly(args.Used, usedPart, targetUid, args.User);
            return;
        }

        // Неверная комбинация — фатальная ошибка
        FailAssembly(targetUid, args.Used, args.User);
    }

    /// <summary>
    /// Успешная сборка: обе детали уничтожаются, спавнится результат.
    /// </summary>
    private void SucceedAssembly(EntityUid baseUid, NCWeaponPartComponent basePart, EntityUid addedUid, EntityUid user)
    {
        var resultId = basePart.CombineResultId;

        // Уничтожаем обе детали
        QueueDel(baseUid);
        QueueDel(addedUid);

        if (!string.IsNullOrEmpty(resultId))
        {
            // Спавним результат в координатах игрока
            var result = Spawn(resultId, Transform(user).Coordinates);
            _popup.PopupEntity("Детали успешно соединены!", user);
        }
        else
        {
            _popup.PopupEntity("Сборка завершена, но результат не определён.", user);
        }
    }

    /// <summary>
    /// Фатальная ошибка сборки: обе детали уничтожаются, спавнится мусор.
    /// </summary>
    private void FailAssembly(EntityUid partA, EntityUid partB, EntityUid user)
    {
        _popup.PopupEntity("ФАТАЛЬНАЯ ОШИБКА! Детали уничтожены!", user);

        // Уничтожаем обе детали
        QueueDel(partA);
        QueueDel(partB);

        // Спавним мусор
        Spawn("Wrench", Transform(user).Coordinates); // Заглушка
    }
}
