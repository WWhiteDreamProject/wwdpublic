using Content.Shared._NC.Crafting.ConsoleCompiler;
using Content.Shared._NC.Decryption.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._NC.Crafting.ConsoleCompiler;

/// <summary>
/// Серверная система консоли-компилятора (Техно-Принтер).
/// Принимает RawData → пополняет баланс данных.
/// Принимает DecryptionTechnology → печатает чертежи или рецепты с DoAfter.
/// При истощении зарядов — сжигает мастер-диск.
/// </summary>
public sealed class ConsoleCompilerSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Регистрация/удаление ItemSlots при инициализации компонента
        SubscribeLocalEvent<ConsoleCompilerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ConsoleCompilerComponent, ComponentRemove>(OnComponentRemove);

        // Вставка предметов через InteractUsing
        SubscribeLocalEvent<ConsoleCompilerComponent, InteractUsingEvent>(OnInteractUsing);

        // Обновление UI при изменении контейнеров
        SubscribeLocalEvent<ConsoleCompilerComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<ConsoleCompilerComponent, EntRemovedFromContainerMessage>(OnContainerModified);

        // UI-открытие
        SubscribeLocalEvent<ConsoleCompilerComponent, BoundUIOpenedEvent>(OnUiOpened);

        // BUI Messages
        SubscribeLocalEvent<ConsoleCompilerComponent, ConsoleCompilerDigitizeMessage>(OnDigitize);
        SubscribeLocalEvent<ConsoleCompilerComponent, ConsoleCompilerEjectReceiverMessage>(OnEjectReceiver);
        SubscribeLocalEvent<ConsoleCompilerComponent, ConsoleCompilerEjectMasterMessage>(OnEjectMaster);
        SubscribeLocalEvent<ConsoleCompilerComponent, ConsoleCompilerPrintMessage>(OnPrint);

        // DoAfter завершение
        SubscribeLocalEvent<ConsoleCompilerComponent, ConsoleCompilerDoAfterEvent>(OnPrintDoAfter);
    }

    // ─── Регистрация ItemSlots (создание backing-контейнеров) ───

    private void OnComponentInit(EntityUid uid, ConsoleCompilerComponent comp, ComponentInit args)
    {
        _itemSlots.AddItemSlot(uid, ConsoleCompilerComponent.ReceiverSlotId, comp.ReceiverSlot);
        _itemSlots.AddItemSlot(uid, ConsoleCompilerComponent.MasterDiskSlotId, comp.MasterDiskSlot);
    }

    private void OnComponentRemove(EntityUid uid, ConsoleCompilerComponent comp, ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(uid, comp.ReceiverSlot);
        _itemSlots.RemoveItemSlot(uid, comp.MasterDiskSlot);
    }

    // ─── InteractUsing: вставка RawData или DecryptionTechnology в слоты ───

    private void OnInteractUsing(EntityUid uid, ConsoleCompilerComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Приоритет: DecryptionTechnology → MasterDiskSlot
        if (HasComp<DecryptionTechnologyComponent>(args.Used))
        {
            if (_itemSlots.TryInsert(uid, comp.MasterDiskSlot, args.Used, args.User))
            {
                args.Handled = true;
                _ui.TryOpenUi(uid, ConsoleCompilerUiKey.Key, args.User);
                UpdateUserInterface(uid, comp);
                return;
            }
        }

        // RawData → ReceiverSlot
        if (HasComp<RawDataComponent>(args.Used))
        {
            if (_itemSlots.TryInsert(uid, comp.ReceiverSlot, args.Used, args.User))
            {
                args.Handled = true;
                _ui.TryOpenUi(uid, ConsoleCompilerUiKey.Key, args.User);
                UpdateUserInterface(uid, comp);
            }
        }
    }

    // ─── Контейнер изменён → обновить UI ───

    private void OnContainerModified(EntityUid uid, ConsoleCompilerComponent comp, ContainerModifiedMessage args)
    {
        UpdateUserInterface(uid, comp);
    }

    // ─── UI открыт → отправить текущее состояние ───

    private void OnUiOpened(EntityUid uid, ConsoleCompilerComponent comp, BoundUIOpenedEvent args)
    {
        UpdateUserInterface(uid, comp);
    }

    // ─── Оцифровка: сжигает RawData из Receiver, прибавляет DataPoints ───

    private void OnDigitize(EntityUid uid, ConsoleCompilerComponent comp, ConsoleCompilerDigitizeMessage args)
    {
        if (args.Actor is not { Valid: true } actor)
            return;

        // Получаем содержимое ReceiverSlot
        var receiverEntity = comp.ReceiverSlot.Item;
        if (receiverEntity == null)
            return;

        // Проверяем наличие RawDataComponent
        if (!TryComp<RawDataComponent>(receiverEntity.Value, out var rawData))
            return;

        // Прибавляем DataPoints к балансу консоли
        comp.AvailableData += rawData.DataPoints;

        // Извлекаем из слота и уничтожаем
        _itemSlots.TryEject(uid, comp.ReceiverSlot, actor, out _);
        QueueDel(receiverEntity.Value);

        _popup.PopupEntity($"+{rawData.DataPoints} данных оцифровано", uid);
        Dirty(uid, comp);
        UpdateUserInterface(uid, comp);
    }

    // ─── Извлечение из Receiver ───

    private void OnEjectReceiver(EntityUid uid, ConsoleCompilerComponent comp, ConsoleCompilerEjectReceiverMessage args)
    {
        if (args.Actor is not { Valid: true } actor)
            return;

        _itemSlots.TryEjectToHands(uid, comp.ReceiverSlot, actor);
        UpdateUserInterface(uid, comp);
    }

    // ─── Извлечение мастер-диска ───

    private void OnEjectMaster(EntityUid uid, ConsoleCompilerComponent comp, ConsoleCompilerEjectMasterMessage args)
    {
        if (args.Actor is not { Valid: true } actor)
            return;

        if (comp.IsPrinting)
            return; // Нельзя извлекать во время печати

        _itemSlots.TryEjectToHands(uid, comp.MasterDiskSlot, actor);
        UpdateUserInterface(uid, comp);
    }

    // ─── Печать: запуск DoAfter ───

    private void OnPrint(EntityUid uid, ConsoleCompilerComponent comp, ConsoleCompilerPrintMessage args)
    {
        if (args.Actor is not { Valid: true } actor)
            return;

        if (comp.IsPrinting)
            return;

        // Проверка наличия мастер-диска
        var masterEntity = comp.MasterDiskSlot.Item;
        if (masterEntity == null)
            return;

        if (!TryComp<DecryptionTechnologyComponent>(masterEntity.Value, out var tech))
            return;

        // Проверка зарядов
        if (tech.RemainingUses <= 0)
            return;

        // Определяем стоимость из технологии
        var cost = args.IsBlueprint ? tech.ModuleCost : tech.RecipeCost;

        // Проверка баланса
        if (comp.AvailableData < cost)
        {
            _popup.PopupEntity("Недостаточно данных!", uid);
            return;
        }

        // Запускаем DoAfter
        comp.IsPrinting = true;

        var ev = new ConsoleCompilerDoAfterEvent(args.IsBlueprint);
        var doAfterArgs = new DoAfterArgs(EntityManager, actor, comp.PrintDoAfterTime, ev, uid, target: uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
        {
            comp.IsPrinting = false;
        }

        Dirty(uid, comp);
        UpdateUserInterface(uid, comp);
    }

    // ─── DoAfter завершён: списание ресурсов, спавн предмета ───

    private void OnPrintDoAfter(EntityUid uid, ConsoleCompilerComponent comp, ConsoleCompilerDoAfterEvent args)
    {
        comp.IsPrinting = false;

        if (args.Handled || args.Cancelled)
        {
            Dirty(uid, comp);
            UpdateUserInterface(uid, comp);
            return;
        }

        args.Handled = true;

        // Повторная проверка мастер-диска (мог быть извлечён)
        var masterEntity = comp.MasterDiskSlot.Item;
        if (masterEntity == null || !TryComp<DecryptionTechnologyComponent>(masterEntity.Value, out var tech))
        {
            Dirty(uid, comp);
            UpdateUserInterface(uid, comp);
            return;
        }

        // Определяем стоимость и прототип
        var cost = args.IsBlueprint ? tech.ModuleCost : tech.RecipeCost;
        var prototype = args.IsBlueprint ? tech.ModulePrototype : tech.RecipePrototype;

        // Финальная проверка баланса
        if (comp.AvailableData < cost || tech.RemainingUses <= 0)
        {
            Dirty(uid, comp);
            UpdateUserInterface(uid, comp);
            return;
        }

        // Списание ресурсов
        comp.AvailableData -= cost;
        tech.RemainingUses--;
        Dirty(masterEntity.Value, tech);
        Dirty(uid, comp);

        // Спавн результата
        if (!string.IsNullOrEmpty(prototype))
        {
            Spawn(prototype, Transform(uid).Coordinates);
            var label = args.IsBlueprint ? "Чертёж" : "Рецепт";
            _popup.PopupEntity($"✔ {label} напечатан!", uid);
        }

        // Проверка истощения мастер-диска
        if (tech.RemainingUses <= 0)
        {
            ExhaustMasterDisk(uid, comp, masterEntity.Value);
        }

        UpdateUserInterface(uid, comp);
    }

    // ─── Истощение мастер-диска: удалить, заспавнить сгоревшую болванку ───

    private void ExhaustMasterDisk(EntityUid uid, ConsoleCompilerComponent comp, EntityUid masterDisk)
    {
        // Извлекаем диск из слота
        _itemSlots.TryEject(uid, comp.MasterDiskSlot, null, out _);

        // Удаляем оригинал
        QueueDel(masterDisk);

        // Спавним сгоревшую болванку на координатах консоли
        Spawn(comp.BurnedDiskPrototype, Transform(uid).Coordinates);

        _popup.PopupEntity("☠ Мастер-диск исчерпан! Болванка сгорела.", uid);
    }

    // ─── Обновление BUI ───

    private void UpdateUserInterface(EntityUid uid, ConsoleCompilerComponent comp)
    {
        if (!_ui.HasUi(uid, ConsoleCompilerUiKey.Key))
            return;

        var hasReceiver = comp.ReceiverSlot.Item != null;
        var hasMaster = comp.MasterDiskSlot.Item != null;

        var masterName = string.Empty;
        var masterUses = 0;
        var blueprintCost = 0;
        var recipeCost = 0;

        if (hasMaster && comp.MasterDiskSlot.Item is { } masterEnt &&
            TryComp<DecryptionTechnologyComponent>(masterEnt, out var tech))
        {
            masterName = MetaData(masterEnt).EntityName;
            masterUses = tech.RemainingUses;
            // Стоимости берём из технологии
            blueprintCost = tech.ModuleCost;
            recipeCost = tech.RecipeCost;
        }

        var state = new ConsoleCompilerBoundUiState(
            comp.AvailableData,
            blueprintCost,
            recipeCost,
            hasReceiver,
            hasMaster,
            masterName,
            masterUses,
            comp.IsPrinting
        );

        _ui.SetUiState(uid, ConsoleCompilerUiKey.Key, state);
    }
}
