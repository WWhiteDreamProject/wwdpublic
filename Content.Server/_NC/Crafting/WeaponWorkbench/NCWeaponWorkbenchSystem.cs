using Content.Shared._NC.Crafting.WeaponWorkbench.Components;
using Content.Shared._NC.Crafting.WeaponWorkbench.Events;
using Content.Shared._NC.Crafting.WeaponWorkbench.Prototypes;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Random;

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

    // Кулдаун кнопок оператора (0.5 сек по диздоку)
    private const float ButtonCooldown = 0.5f;

    // Длительность красного мигания экрана (секунды)
    private const float FlashDuration = 0.4f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NCWeaponWorkbenchComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NCWeaponWorkbenchComponent, InteractUsingEvent>(OnInteractUsing);

        // BUI messages
        SubscribeLocalEvent<NCWeaponWorkbenchComponent, NCWorkbenchOperatorCommandMessage>(OnOperatorCommand);
        SubscribeLocalEvent<NCWeaponWorkbenchComponent, NCWorkbenchLockCodeInputMessage>(OnLockCodeInput);
    }

    private void OnInit(EntityUid uid, NCWeaponWorkbenchComponent component, ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(uid, NCWeaponWorkbenchComponent.MaterialSlotId);
    }

    private void OnInteractUsing(EntityUid uid, NCWeaponWorkbenchComponent component, InteractUsingEvent args)
    {
        if (component.State != NCWeaponWorkbenchState.Idle)
            return;

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

    /// <summary>
    /// Обработка команд оператора. Блокируется при кулдауне и при системной блокировке.
    /// </summary>
    private void OnOperatorCommand(EntityUid uid, NCWeaponWorkbenchComponent component, NCWorkbenchOperatorCommandMessage args)
    {
        // Во время системной блокировки кнопки оператора не работают
        if (component.IsSystemLocked)
            return;

        // Кулдаун кнопок — нельзя спамить (кроме Start)
        if (args.CommandType != OperatorCommandType.StartScraping && component.ButtonCooldownTimer > 0f)
            return;

        switch (args.CommandType)
        {
            case OperatorCommandType.StartScraping:
                TryStartCycle(uid, component);
                break;
            case OperatorCommandType.ApplyCoolant:
                // Сдвигает Heat к центру (0.5)
                component.Heat = Math.Max(0.5f, component.Heat - 0.2f);
                component.ButtonCooldownTimer = ButtonCooldown;
                break;
            case OperatorCommandType.SpotWeld:
                // Сдвигает Integrity к центру (0.5)
                component.Integrity = Math.Min(0.5f, component.Integrity + 0.2f);
                component.ButtonCooldownTimer = ButtonCooldown;
                break;
            case OperatorCommandType.AlignLeft:
                component.Alignment = Math.Max(0f, component.Alignment - 0.15f);
                component.ButtonCooldownTimer = ButtonCooldown;
                break;
            case OperatorCommandType.AlignRight:
                component.Alignment = Math.Min(1f, component.Alignment + 0.15f);
                component.ButtonCooldownTimer = ButtonCooldown;
                break;
        }

        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Обработка ввода кода системной блокировки (Тир 3).
    /// </summary>
    private void OnLockCodeInput(EntityUid uid, NCWeaponWorkbenchComponent component, NCWorkbenchLockCodeInputMessage args)
    {
        if (!component.IsSystemLocked)
            return;

        if (args.Code == component.LockCode)
        {
            // Код верный — блокировка снята
            component.IsSystemLocked = false;
            component.WarningMessage = "✔ ACCESS CODE ACCEPTED.";
        }
        else
        {
            // Неверный код — оповещаем но не фейлим
            component.WarningMessage = "✖ INVALID CODE. TRY AGAIN.";
        }

        UpdateUserInterface(uid, component);
    }

    private void TryStartCycle(EntityUid uid, NCWeaponWorkbenchComponent component)
    {
        if (component.State != NCWeaponWorkbenchState.Idle)
            return;

        var materialContainer = (ContainerSlot) _container.GetContainer(uid, NCWeaponWorkbenchComponent.MaterialSlotId);
        if (materialContainer.ContainedEntity == null)
            return;

        // Сброс всех параметров мини-игры
        component.State = NCWeaponWorkbenchState.Processing;
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

        UpdateUserInterface(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NCWeaponWorkbenchComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Тикаем кулдаун кнопок всегда
            if (comp.ButtonCooldownTimer > 0f)
                comp.ButtonCooldownTimer = Math.Max(0f, comp.ButtonCooldownTimer - frameTime);

            // Тикаем таймер красного мигания
            if (comp.FlashTimer > 0f)
                comp.FlashTimer = Math.Max(0f, comp.FlashTimer - frameTime);

            if (comp.State != NCWeaponWorkbenchState.Processing)
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
                    comp.WarningMessage = "⚠ SYSTEM LOCK: ENTER ACCESS CODE";
                    comp.FlashTimer = FlashDuration;
                }
            }

            // === Проверка критического состояния ===
            bool isCritical = Math.Abs(comp.Heat - 0.5f) > comp.CurrentSafeZoneHalf ||
                              Math.Abs(comp.Integrity - 0.5f) > comp.CurrentSafeZoneHalf ||
                              Math.Abs(comp.Alignment - 0.5f) > comp.CurrentSafeZoneHalf;

            if (isCritical)
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

        switch (anomaly)
        {
            case NCWorkbenchAnomalyType.HeatSpike:
                component.Heat = 1.0f;
                component.WarningMessage = "⚠ WARNING: THERMAL RUNAWAY!";
                break;
            case NCWorkbenchAnomalyType.IntegrityDrop:
                component.Integrity = 0.0f;
                component.WarningMessage = "⚠ WARNING: STRUCTURAL STRESS!";
                break;
            case NCWorkbenchAnomalyType.AlignmentLeft:
                component.Alignment = 0.0f;
                component.WarningMessage = "⚠ WARNING: OPTICS MISALIGNED!";
                break;
            case NCWorkbenchAnomalyType.AlignmentRight:
                component.Alignment = 1.0f;
                component.WarningMessage = "⚠ WARNING: OPTICS MISALIGNED!";
                break;
            case NCWorkbenchAnomalyType.DoubleTrouble:
                component.Heat = 1.0f;
                component.Integrity = 0.0f;
                component.WarningMessage = "☠ CRITICAL: MULTIPLE SYSTEM FAILURES!";
                break;
        }
    }

    private void FailCrafting(EntityUid uid, NCWeaponWorkbenchComponent component)
    {
        component.State = NCWeaponWorkbenchState.Failed;
        component.WarningMessage = "☠ CRITICAL FAILURE! SCRAP PRODUCED.";

        var materialContainer = (ContainerSlot) _container.GetContainer(uid, NCWeaponWorkbenchComponent.MaterialSlotId);
        if (materialContainer.ContainedEntity != null)
            QueueDel(materialContainer.ContainedEntity.Value);

        Spawn("Wrench", Transform(uid).Coordinates); // Заглушка-мусор

        component.State = NCWeaponWorkbenchState.Idle;
        UpdateUserInterface(uid, component);
    }

    private void SucceedCrafting(EntityUid uid, NCWeaponWorkbenchComponent component)
    {
        component.State = NCWeaponWorkbenchState.Success;
        component.WarningMessage = "✔ CRAFTING COMPLETE.";

        var materialContainer = (ContainerSlot) _container.GetContainer(uid, NCWeaponWorkbenchComponent.MaterialSlotId);
        if (materialContainer.ContainedEntity != null)
            QueueDel(materialContainer.ContainedEntity.Value);

        var resultId = !string.IsNullOrEmpty(component.ResultEntityId)
            ? component.ResultEntityId
            : "Wrench";

        Spawn(resultId, Transform(uid).Coordinates);

        component.State = NCWeaponWorkbenchState.Idle;
        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, NCWeaponWorkbenchComponent component)
    {
        if (!_uiSystem.HasUi(uid, NCWeaponWorkbenchUiKey.Key))
            return;

        var materialContainer = (ContainerSlot) _container.GetContainer(uid, NCWeaponWorkbenchComponent.MaterialSlotId);

        var state = new NCWeaponWorkbenchUpdateState(
            component.State,
            component.Heat,
            component.Integrity,
            component.Alignment,
            component.Progress,
            component.CurrentSafeZoneHalf,
            component.WarningMessage,
            materialContainer.ContainedEntity != null,
            component.FlashTimer > 0f,
            component.ButtonCooldownTimer,
            component.IsSystemLocked,
            component.IsSystemLocked ? component.LockCode : null
        );

        _uiSystem.SetUiState(uid, NCWeaponWorkbenchUiKey.Key, state);
    }
}
