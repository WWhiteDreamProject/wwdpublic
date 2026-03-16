using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Shared._NC.Crafting.WeaponWorkbench.Components;
using Content.Shared._NC.Crafting.WeaponWorkbench.Events;
using Content.Shared._NC.Crafting.WeaponWorkbench.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using System;

namespace Content.Server._NC.Crafting.WeaponWorkbench;

/// <summary>
/// Серверная система ЧПУ-верстака. Управляет мини-игрой калибровки:
/// дрейф датчиков, срабатывание аномалий, кулдаун кнопок,
/// красное мигание, системная блокировка (Тир 3), провал/успех.
/// </summary>
public sealed partial class NCWeaponWorkbenchSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    // Кулдаун кнопок оператора (0.5 сек по диздоку)
    private const float ButtonCooldown = 0.5f;

    // Длительность красного мигания экрана (секунды)
    private const float FlashDuration = 0.4f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NCWeaponWorkbenchComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NCWeaponWorkbenchComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<NCWeaponWorkbenchComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<NCWeaponWorkbenchComponent, PowerChangedEvent>(OnPowerChanged);

        // BUI messages
        SubscribeLocalEvent<NCWeaponWorkbenchComponent, NCWorkbenchOperatorCommandMessage>(OnOperatorCommand);
        SubscribeLocalEvent<NCWeaponWorkbenchComponent, NCWorkbenchLockCodeInputMessage>(OnLockCodeInput);
    }

    private void OnInit(EntityUid uid, NCWeaponWorkbenchComponent component, ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(uid, NCWeaponWorkbenchComponent.MaterialSlotId);

        UpdateAppearance(uid, component);
    }

    private void OnInteractUsing(EntityUid uid, NCWeaponWorkbenchComponent component, InteractUsingEvent args)
    {
        if (component.State != NCWeaponWorkbenchState.Idle)
            return;

        if (!_power.IsPowered(uid))
        {
            _popup.PopupEntity(Loc.GetString("nc-workbench-no-power"), uid, args.User);
            return;
        }

        var materialContainer = (ContainerSlot) _container.GetContainer(uid, NCWeaponWorkbenchComponent.MaterialSlotId);

        if (materialContainer.ContainedEntity == null)
        {
            if (_container.Insert(args.Used, materialContainer))
            {
                args.Handled = true;
                _uiSystem.TryOpenUi(uid, NCWeaponWorkbenchUiKey.Key, args.User);
                UpdateUserInterface(uid, component);
            }
        }
    }

    private void OnInteractHand(EntityUid uid, NCWeaponWorkbenchComponent component, InteractHandEvent args)
    {
        if (component.State != NCWeaponWorkbenchState.Idle)
            return;

        var materialContainer = (ContainerSlot) _container.GetContainer(uid, NCWeaponWorkbenchComponent.MaterialSlotId);

        if (materialContainer.ContainedEntity != null)
        {
            _container.Remove(materialContainer.ContainedEntity.Value, materialContainer);
            args.Handled = true;
            UpdateUserInterface(uid, component);
        }
    }

    private void OnPowerChanged(EntityUid uid, NCWeaponWorkbenchComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            _uiSystem.CloseUi(uid, NCWeaponWorkbenchUiKey.Key);
        }

        UpdateAppearance(uid, component);
        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Обработка команд оператора. Блокируется при системной блокировке.
    /// </summary>
    private void OnOperatorCommand(EntityUid uid, NCWeaponWorkbenchComponent component, NCWorkbenchOperatorCommandMessage args)
    {
        // Проверка питания
        if (!_power.IsPowered(uid))
            return;

        // Во время системной блокировки кнопки оператора не работают
        if (component.IsSystemLocked)
            return;

        switch (args.CommandType)
        {
            case OperatorCommandType.StartScraping:
                TryStartCycle(uid, component);
                break;
            case OperatorCommandType.ApplyCoolant:
                // Сдвигает Heat к нулю
                component.Heat = Math.Max(0.0f, component.Heat - 0.2f);
                break;
            case OperatorCommandType.SpotWeld:
                // Сдвигает Integrity к максимуму
                component.Integrity = Math.Min(1.0f, component.Integrity + 0.2f);
                break;
            case OperatorCommandType.AlignLeft:
                component.Alignment = Math.Max(0.0f, component.Alignment - 0.15f);
                break;
            case OperatorCommandType.AlignRight:
                component.Alignment = Math.Min(1.0f, component.Alignment + 0.15f);
                break;
        }

        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Обработка ввода кода системной блокировки (Тир 3).
    /// </summary>
    private void OnLockCodeInput(EntityUid uid, NCWeaponWorkbenchComponent component, NCWorkbenchLockCodeInputMessage args)
    {
        if (!component.IsSystemLocked || !_power.IsPowered(uid))
            return;

        if (args.Code == component.LockCode)
        {
            // Код верный — блокировка снята
            component.IsSystemLocked = false;
            component.WarningMessage = Loc.GetString("nc-workbench-system-access-accepted");
        }
        else
        {
            // Неверный код — оповещаем но не фейлим
            component.WarningMessage = Loc.GetString("nc-workbench-system-invalid-code");
        }

        UpdateUserInterface(uid, component);
    }

    private void SetState(EntityUid uid, NCWeaponWorkbenchComponent component, NCWeaponWorkbenchState state)
    {
        component.State = state;
        UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, NCWeaponWorkbenchComponent component)
    {
        if (!_power.IsPowered(uid))
        {
            _appearance.SetData(uid, NCWeaponWorkbenchVisuals.State, NCWeaponWorkbenchState.Unpowered);
        }
        else
        {
            _appearance.SetData(uid, NCWeaponWorkbenchVisuals.State, component.State);
        }
    }

    private void TryStartCycle(EntityUid uid, NCWeaponWorkbenchComponent component)
    {
        if (component.State != NCWeaponWorkbenchState.Idle || !_power.IsPowered(uid))
            return;

        var materialContainer = (ContainerSlot) _container.GetContainer(uid, NCWeaponWorkbenchComponent.MaterialSlotId);
        if (materialContainer.ContainedEntity == null)
            return;

        // Копируем настройки из болванки, если компонент присутствует
        if (TryComp<NCWeaponBlankComponent>(materialContainer.ContainedEntity.Value, out var blank))
        {
            component.CurrentSafeZoneHalf = blank.SafeZoneHalf;
            component.ToleranceTime = blank.ToleranceTime;
            component.ProgressSpeed = blank.ProgressSpeed;
            component.Anomalies = new Dictionary<float, NCWorkbenchAnomalyType>(blank.Anomalies);
            component.ResultEntityId = blank.ResultEntityId;
            component.EnableSystemLock = blank.EnableSystemLock;
        }

        // Сброс всех параметров мини-игры
        SetState(uid, component, NCWeaponWorkbenchState.Processing);
        component.Progress = 0f;
        component.Heat = 0.5f;
        component.Integrity = 0.5f;
        component.Alignment = 0.5f;
        component.CriticalTimer = 0f;
        component.ButtonCooldownTimer = 0f;
        component.FlashTimer = 0f;
        component.WarningMessage = string.Empty;
        component.TriggeredAnomalies.Clear();
        component.IsSystemLocked = false;
        component.LockCode = string.Empty;
        component.LockTriggered = false;
        component.AnomalyGraceTimer = 0f;

        UpdateUserInterface(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NCWeaponWorkbenchComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Тикаем кулдаун кнопок всегда (если есть питание?)
            // В принципе можно и без питания тикать кулдаун, но логичнее только при питании.
            // Однако IsPowered может быть затратным в Update, так что проверим только для Processing.
            if (comp.ButtonCooldownTimer > 0f)
                comp.ButtonCooldownTimer = Math.Max(0f, comp.ButtonCooldownTimer - frameTime);

            // Тикаем таймер красного мигания
            if (comp.FlashTimer > 0f)
                comp.FlashTimer = Math.Max(0f, comp.FlashTimer - frameTime);

            // Тикаем таймер "окна безопасности"
            if (comp.AnomalyGraceTimer > 0f)
                comp.AnomalyGraceTimer = Math.Max(0f, comp.AnomalyGraceTimer - frameTime);

            if (comp.State != NCWeaponWorkbenchState.Processing)
                continue;

            // Если питание пропало во время работы — паузим или просто не тикаем прогресс.
            if (!_power.IsPowered(uid))
                continue;

            // === Естественный дрейф датчиков (даже во время блокировки!) ===
            comp.Heat += 0.02f * frameTime;
            comp.Integrity -= 0.02f * frameTime;

            if (_random.Prob(0.1f))
                comp.Alignment += _random.NextFloat(-0.05f, 0.05f);

            comp.Heat = Math.Clamp(comp.Heat, 0f, 1f);
            comp.Integrity = Math.Clamp(comp.Integrity, 0f, 1f);
            comp.Alignment = Math.Clamp(comp.Alignment, 0f, 1f);

            // === Прогресс (НЕ останавливается при блокировке) ===
            comp.Progress += comp.ProgressSpeed * frameTime;

            // === Аномалии ===
            foreach (var (progressKey, anomaly) in comp.Anomalies)
            {
                if (comp.Progress >= progressKey && !comp.TriggeredAnomalies.Contains(progressKey))
                {
                    TriggerAnomaly(uid, comp, anomaly);
                    comp.TriggeredAnomalies.Add(progressKey);
                }
            }

            // === Системная блокировка (Тир 3) ===
            // Срабатывает в случайный момент один раз за цикл
            if (comp.EnableSystemLock && !comp.LockTriggered && !comp.IsSystemLocked)
            {
                // Вероятность ~2% за тик (при 60fps ≈ через 0.8 сек)
                if (comp.Progress > 0.2f && _random.Prob(0.02f))
                {
                    comp.IsSystemLocked = true;
                    comp.LockTriggered = true;
                    comp.LockCode = _random.Next(1000, 9999).ToString();
                    comp.WarningMessage = Loc.GetString("nc-workbench-system-lock-warning");
                    comp.FlashTimer = FlashDuration;
                }
            }

            // === Проверка критического состояния (Красная зона) ===
            // Таймер смерти включается только если датчик почти в самом конце шкал (отклонение > 0.45)
            // Это дает игроку шанс спасти деталь одним кликом.
            bool isDeadly = Math.Abs(comp.Heat - 0.5f) > 0.45f ||
                             Math.Abs(comp.Integrity - 0.5f) > 0.45f ||
                             Math.Abs(comp.Alignment - 0.5f) > 0.45f;

            // Если активно "окно безопасности", CriticalTimer не тикает
            if (isDeadly && comp.AnomalyGraceTimer <= 0f)
            {
                comp.CriticalTimer += frameTime;
                if (comp.CriticalTimer > comp.ToleranceTime)
                {
                    FailCrafting(uid, comp);
                    continue;
                }
            }
            else
            {
                // Если мы вышли из красной зоны (даже если мы всё еще в желтой), таймер сбрасывается.
                comp.CriticalTimer = 0f;
            }

            // === Успех ===
            if (comp.Progress >= 1f)
            {
                SucceedCrafting(uid, comp);
                continue;
            }

            UpdateUserInterface(uid, comp);
        }
    }

    /// <summary>
    /// Аномалия: мгновенный рывок датчика + красное мигание экрана.
    /// </summary>
    private void TriggerAnomaly(EntityUid uid, NCWeaponWorkbenchComponent component, NCWorkbenchAnomalyType anomaly)
    {
        // Включаем красное мигание
        component.FlashTimer = FlashDuration;
        // Даем окно безопасности, чтобы игрок успел среагировать
        component.AnomalyGraceTimer = 0.8f;
        component.CriticalTimer = 0f;

        switch (anomaly)
        {
            case NCWorkbenchAnomalyType.HeatSpike:
                component.Heat = 0.93f; // Смягчено с 1.0f
                component.WarningMessage = Loc.GetString("nc-workbench-system-anomaly-heat");
                break;
            case NCWorkbenchAnomalyType.IntegrityDrop:
                component.Integrity = 0.07f; // Смягчено с 0.0f
                component.WarningMessage = Loc.GetString("nc-workbench-system-anomaly-integrity");
                break;
            case NCWorkbenchAnomalyType.AlignmentLeft:
                component.Alignment = 0.07f;
                component.WarningMessage = Loc.GetString("nc-workbench-system-anomaly-alignment");
                break;
            case NCWorkbenchAnomalyType.AlignmentRight:
                component.Alignment = 0.93f;
                component.WarningMessage = Loc.GetString("nc-workbench-system-anomaly-alignment");
                break;
            case NCWorkbenchAnomalyType.DoubleTrouble:
                component.Heat = 0.93f;
                component.Integrity = 0.07f;
                component.WarningMessage = Loc.GetString("nc-workbench-system-anomaly-critical");
                break;
        }
    }

    private void FailCrafting(EntityUid uid, NCWeaponWorkbenchComponent component)
    {
        SetState(uid, component, NCWeaponWorkbenchState.Failed);
        component.WarningMessage = Loc.GetString("nc-workbench-system-failure");

        var materialContainer = (ContainerSlot) _container.GetContainer(uid, NCWeaponWorkbenchComponent.MaterialSlotId);
        if (materialContainer.ContainedEntity != null)
            QueueDel(materialContainer.ContainedEntity.Value);

        Spawn("NCWeaponScrap", Transform(uid).Coordinates);

        SetState(uid, component, NCWeaponWorkbenchState.Idle);
        UpdateUserInterface(uid, component);
    }

    private void SucceedCrafting(EntityUid uid, NCWeaponWorkbenchComponent component)
    {
        SetState(uid, component, NCWeaponWorkbenchState.Success);
        component.WarningMessage = Loc.GetString("nc-workbench-system-success");

        var materialContainer = (ContainerSlot) _container.GetContainer(uid, NCWeaponWorkbenchComponent.MaterialSlotId);
        if (materialContainer.ContainedEntity != null)
            QueueDel(materialContainer.ContainedEntity.Value);

        var resultId = !string.IsNullOrEmpty(component.ResultEntityId)
            ? component.ResultEntityId
            : "NCWeaponScrap";

        Spawn(resultId, Transform(uid).Coordinates);

        SetState(uid, component, NCWeaponWorkbenchState.Idle);
        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, NCWeaponWorkbenchComponent component)
    {
        if (!_uiSystem.HasUi(uid, NCWeaponWorkbenchUiKey.Key))
            return;

        var materialContainer = (ContainerSlot) _container.GetContainer(uid, NCWeaponWorkbenchComponent.MaterialSlotId);
        string? sourceProto = null;
        if (materialContainer.ContainedEntity is { } materialent)
        {
            sourceProto = MetaData(materialent).EntityPrototype?.ID;
        }

        var state = new NCWeaponWorkbenchUpdateState(
            component.State,
            component.Heat,
            component.Integrity,
            component.Alignment,
            component.Progress,
            component.CurrentSafeZoneHalf,
            component.WarningMessage,
            materialContainer.ContainedEntity != null,
            sourceProto,
            component.ResultEntityId,
            component.FlashTimer > 0f,
            component.ButtonCooldownTimer,
            component.IsSystemLocked,
            component.IsSystemLocked ? component.LockCode : null
        );

        _uiSystem.SetUiState(uid, NCWeaponWorkbenchUiKey.Key, state);
    }
}
