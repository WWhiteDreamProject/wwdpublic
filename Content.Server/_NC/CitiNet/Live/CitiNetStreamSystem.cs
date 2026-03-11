using Content.Server._NC.Bank;
using Content.Server.CartridgeLoader;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Server.SurveillanceCamera;
using Content.Shared._NC.CitiNet.Components;
using Content.Shared._NC.CitiNet.Live;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.SurveillanceCamera;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Server.Player;
using Robust.Shared.Timing;
using Content.Server._NC.CitiNet.Cartridges;

namespace Content.Server._NC.CitiNet.Live;

/// <summary>
/// NC — Серверная система CitiNet Live.
/// Управляет стримами: запуск/стоп, расход батареи,
/// обрыв при смерти, лимит слотов, донаты, SurveillanceCamera.
/// </summary>
public sealed class CitiNetStreamSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SurveillanceCameraSystem _camera = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly CitiNetLiveCartridgeSystem _liveCartridge = default!;

    /// <summary>Глобальный список активных стримов (EntityUid камеры).</summary>
    private readonly HashSet<EntityUid> _activeStreams = new();

    /// <summary>Максимальное число одновременных стримов на сервере.</summary>
    public const int MaxActiveStreams = 5;

    private const float DrainInterval = 1.0f;
    private float _drainTimer;

    public override void Initialize()
    {
        base.Initialize();

        // Обрыв стрима при удалении камеры
        SubscribeLocalEvent<StreamCamComponent, ComponentShutdown>(OnCamShutdown);

        // Обрыв стрима при смерти — ловим через InventoryComponent (т.к. HandsComponent занят в CitiNetCartridgeSystem)
        SubscribeLocalEvent<InventoryComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _drainTimer += frameTime;
        if (_drainTimer < DrainInterval)
            return;
        _drainTimer -= DrainInterval;

        // Дренируем батарею всех активных стримов
        var query = EntityQueryEnumerator<StreamCamComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.IsStreaming)
                continue;

            // Проверяем CitiNet Relay
            if (!HasActiveCitiNetRelay(uid))
            {
                StopStream(uid, comp, "citinet-live-no-relay");
                continue;
            }

            // Дренаж батареи
            if (!_powerCell.TryUseCharge(uid, comp.EnergyDrainRate))
                StopStream(uid, comp, "citinet-live-battery-dead");
        }
    }

    // =====================================================
    //   Публичный API (вызывается CitiNetCartridgeSystem)
    // =====================================================

    /// <summary>
    /// Попытка запустить стрим с камеры.
    /// Возвращает false + reason при отказе.
    /// </summary>
    public bool TryStartStream(EntityUid cam, string title, out string? errorLocKey, StreamCamComponent? comp = null)
    {
        errorLocKey = null;

        if (!Resolve(cam, ref comp))
        {
            errorLocKey = "citinet-live-no-cam";
            return false;
        }

        if (comp.IsStreaming)
            return false;

        if (_activeStreams.Count >= MaxActiveStreams)
        {
            errorLocKey = "citinet-live-network-full";
            return false;
        }

        if (!HasActiveCitiNetRelay(cam))
        {
            errorLocKey = "citinet-live-no-relay";
            return false;
        }

        if (!_powerCell.HasCharge(cam, comp.EnergyDrainRate))
        {
            errorLocKey = "citinet-live-battery-dead";
            return false;
        }

        comp.StreamTitle = title;
        comp.IsStreaming = true;
        comp.ViewerCount = 0;
        comp.ChatMessages.Clear();
        _activeStreams.Add(cam);

        // Активируем SurveillanceCamera для ViewSubscriber зрителей
        if (TryComp<SurveillanceCameraComponent>(cam, out var camComp))
            _camera.SetActive(cam, true, camComp);

        _liveCartridge.UpdateAllLiveUIs();
        return true;
    }

    /// <summary>Остановить стрим и уведомить зрителей.</summary>
    public void StopStream(EntityUid cam, StreamCamComponent? comp = null, string? reasonLocKey = null)
    {
        if (!Resolve(cam, ref comp, false) || !comp.IsStreaming)
            return;

        comp.IsStreaming = false;
        _activeStreams.Remove(cam);

        // Деактивируем камеру — это отписывает зрителей
        if (TryComp<SurveillanceCameraComponent>(cam, out var camComp))
            _camera.SetActive(cam, false, camComp);

        // Системное сообщение в историю чата
        var reason = reasonLocKey != null
            ? Loc.GetString(reasonLocKey)
            : Loc.GetString("citinet-live-signal-lost");
        AddChatMessage(comp, new LiveChatMessage(_timing.CurTime, Loc.GetString("citinet-live-sender-system"), reason, true));

        _liveCartridge.UpdateAllLiveUIs();
    }

    /// <summary>Подключить зрителя к SurveillanceCamera стрима.</summary>
    public bool TryAddViewer(EntityUid cam, EntityUid viewer, StreamCamComponent? comp = null)
    {
        if (!Resolve(cam, ref comp, false) || !comp.IsStreaming)
            return false;

        if (!TryComp<SurveillanceCameraComponent>(cam, out var camComp))
            return false;

        _camera.AddActiveViewer(cam, viewer, null, camComp);
        comp.ViewerCount++;
        _liveCartridge.UpdateAllLiveUIs();
        return true;
    }

    /// <summary>Отключить зрителя.</summary>
    public void RemoveViewer(EntityUid cam, EntityUid viewer, StreamCamComponent? comp = null)
    {
        if (!Resolve(cam, ref comp, false))
            return;

        if (!TryComp<SurveillanceCameraComponent>(cam, out var camComp))
            return;

        _camera.RemoveActiveViewer(cam, viewer, null, camComp);
        comp.ViewerCount = Math.Max(0, comp.ViewerCount - 1);
        _liveCartridge.UpdateAllLiveUIs();
    }

    /// <summary>
    /// Перевести донат от зрителя к стримеру.
    /// Снимает со счёта зрителя, кладёт стримеру, вызывает попап.
    /// </summary>
    public bool SendDonation(EntityUid viewer, EntityUid cam, int amount, string message,
        StreamCamComponent? comp = null)
    {
        if (!Resolve(cam, ref comp, false) || !comp.IsStreaming)
            return false;

        if (amount <= 0)
            return false;

        // Списать у зрителя
        if (!_bank.TryBankWithdraw(viewer, amount))
            return false;

        // Начислить стримеру (владельцу камеры)
        if (comp.HolderUid.HasValue)
            _bank.TryBankDeposit(comp.HolderUid.Value, amount);

        // Уведомление стримеру (экранируем спецсимволы markup)
        var escapedMessage = Robust.Shared.Utility.FormattedMessage.EscapeText(message);
        var donatePopup = Loc.GetString("citinet-live-donate-received",
            ("amount", amount),
            ("message", escapedMessage));

        if (comp.HolderUid.HasValue)
            _popup.PopupEntity(donatePopup, comp.HolderUid.Value, comp.HolderUid.Value);

        // Запись в чат
        var chatMsg = new LiveChatMessage(
            _timing.CurTime,
            Loc.GetString("citinet-live-donate-chat-prefix"),
            $"${amount} — {message}",
            true);
        AddChatMessage(comp, chatMsg);

        return true;
    }

    /// <summary>Добавить сообщение в чат стрима.</summary>
    public void AddChatMessage(StreamCamComponent comp, LiveChatMessage message)
    {
        comp.ChatMessages.Add(message);
        if (comp.ChatMessages.Count > comp.MaxChatMessages)
            comp.ChatMessages.RemoveAt(0);
    }

    /// <summary>Получить список всех активных стримов для UI-листа.</summary>
    public IEnumerable<EntityUid> GetActiveStreams() => _activeStreams;

    // =====================================================
    //   Вспомогательные методы
    // =====================================================

    private void OnCamShutdown(EntityUid uid, StreamCamComponent comp, ComponentShutdown args)
    {
        if (comp.IsStreaming)
            StopStream(uid, comp, "citinet-live-signal-lost");
    }

    private void OnMobStateChanged(EntityUid uid, InventoryComponent _, MobStateChangedEvent args)
    {
        // Ищем камеру в руках / надетую на персонажа
        if (args.NewMobState != MobState.Dead && args.NewMobState != MobState.Critical)
            return;

        // Проверяем все камеры, чьим владельцем является этот моб
        var query = EntityQueryEnumerator<StreamCamComponent>();
        while (query.MoveNext(out var cam, out var comp))
        {
            if (comp.HolderUid == uid && comp.IsStreaming)
                StopStream(cam, comp, "citinet-live-signal-lost");
        }
    }

    /// <summary>
    /// Проверяет наличие работающего CitiNet Relay на той же карте, что и камера.
    /// </summary>
    public bool HasActiveCitiNetRelay(EntityUid cam)
    {
        var mapCoords = _transform.GetMapCoordinates(cam);

        if (mapCoords.MapId == Robust.Shared.Map.MapId.Nullspace)
            return false;

        var query = EntityQueryEnumerator<CitiNetRelayComponent, Content.Server.Power.Components.ApcPowerReceiverComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var power, out var xform))
        {
            if (xform.MapID == mapCoords.MapId && power.Powered)
                return true;
        }

        return false;
    }
}
