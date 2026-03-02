using Content.Shared._NC.CitiNet.Components;
using Content.Shared.Interaction;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server._NC.CitiNet.Systems;

/// <summary>
/// Система BurnerChip — одноразовые анонимные чипы.
/// При использовании на PDA подменяет OwnerName на сгенерированный временный ID.
/// При извлечении/уничтожении — восстанавливает оригинальное имя.
/// Повторное использование невозможно.
/// </summary>
public sealed class BurnerChipSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BurnerChipComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BurnerChipComponent, ComponentShutdown>(OnShutdown);
    }

    /// <summary>
    /// Обработка использования BurnerChip на PDA.
    /// </summary>
    private void OnAfterInteract(Entity<BurnerChipComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        // Целевая сущность должна быть PDA
        if (!TryComp<PdaComponent>(args.Target, out var pda))
            return;

        // Чип уже был использован
        if (ent.Comp.IsUsed)
        {
            _popup.PopupEntity(Loc.GetString("citinet-burner-chip-used"), args.Target.Value, args.User);
            return;
        }

        // Генерируем временный ID если ещё не создан
        if (string.IsNullOrEmpty(ent.Comp.TempId))
        {
            ent.Comp.TempId = GenerateTempId();
        }

        // Сохраняем оригинальное имя
        ent.Comp.OriginalOwnerName = pda.OwnerName;
        ent.Comp.InsertedInto = args.Target.Value;
        ent.Comp.IsUsed = true;

        // Подменяем имя
        pda.OwnerName = ent.Comp.TempId;
        Dirty(args.Target.Value, pda);

        _popup.PopupEntity(
            Loc.GetString("citinet-burner-chip-inserted", ("id", ent.Comp.TempId)),
            args.Target.Value,
            args.User);

        args.Handled = true;
    }

    /// <summary>
    /// При уничтожении чипа восстанавливаем оригинальное имя PDA.
    /// </summary>
    private void OnShutdown(Entity<BurnerChipComponent> ent, ref ComponentShutdown args)
    {
        RestoreOwnerName(ent);
    }

    /// <summary>
    /// Восстанавливает оригинальное имя владельца PDA.
    /// </summary>
    private void RestoreOwnerName(Entity<BurnerChipComponent> ent)
    {
        if (ent.Comp.InsertedInto == null || ent.Comp.OriginalOwnerName == null)
            return;

        if (!TryComp<PdaComponent>(ent.Comp.InsertedInto, out var pda))
            return;

        // Восстанавливаем только если текущее имя совпадает с нашим подменённым
        if (pda.OwnerName == ent.Comp.TempId)
        {
            pda.OwnerName = ent.Comp.OriginalOwnerName;
            Dirty(ent.Comp.InsertedInto.Value, pda);
        }

        ent.Comp.InsertedInto = null;
    }

    /// <summary>
    /// Генерирует анонимный временный ID вида "BURNER-XXXX".
    /// </summary>
    private string GenerateTempId()
    {
        return $"BURNER-{_random.Next(1000, 9999)}";
    }
}
