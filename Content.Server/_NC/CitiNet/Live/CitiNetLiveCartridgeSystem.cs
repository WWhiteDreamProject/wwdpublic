// NC — Серверная система картриджа CitiNet Live.
// Отдельное приложение PDA для стриминга через CitiNet.

using Content.Server._NC.Bank;
using Content.Server.CartridgeLoader;
using Content.Server.PowerCell;
using Content.Shared._NC.CitiNet;
using Content.Shared._NC.CitiNet.Live;
using Content.Shared.CartridgeLoader;
using Content.Shared.Inventory;
using Robust.Shared.Timing;

namespace Content.Server._NC.CitiNet.Live;

public sealed class CitiNetLiveCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CitiNetStreamSystem _liveStream = default!;
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CitiNetLiveCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<CitiNetLiveCartridgeComponent, CartridgeMessageEvent>(OnMessage);
        SubscribeLocalEvent<CitiNetLiveCartridgeComponent, CartridgeDeactivatedEvent>(OnDeactivated);
    }

    // ===================== EVENTS =====================

    private void OnUiReady(Entity<CitiNetLiveCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUI(ent, args.Loader);
    }

    private void OnDeactivated(Entity<CitiNetLiveCartridgeComponent> ent, ref CartridgeDeactivatedEvent args)
    {
        // При закрытии картриджа — отписываемся от просмотра
        var ownerUid = GetPdaHolderUid(ent);
        if (ownerUid != null && ent.Comp.WatchedCamUid.HasValue)
        {
            _liveStream.RemoveViewer(ent.Comp.WatchedCamUid.Value, ownerUid.Value);
            ent.Comp.WatchedCamUid = null;
        }
    }

    private void OnMessage(Entity<CitiNetLiveCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not CitiNetLiveMessageEvent msg)
            return;

        var ownerUid = GetPdaHolderUid(ent);
        if (ownerUid == null)
            return;

        var cam = FindStreamCam(ownerUid.Value);

        switch (msg.Type)
        {
            case CitiNetLiveMessageType.StartStream:
                if (!cam.HasValue) return;
                var title = string.IsNullOrWhiteSpace(msg.Content) ? Loc.GetString("citinet-live-stream-default-title") : msg.Content;
                if (_liveStream.TryStartStream(cam.Value, title, out _))
                {
                    if (TryComp<StreamCamComponent>(cam.Value, out var camComp))
                        camComp.HolderUid = ownerUid;
                    UpdateAllLiveUIs();
                }
                break;

            case CitiNetLiveMessageType.StopStream:
                if (cam.HasValue)
                {
                    _liveStream.StopStream(cam.Value);
                    UpdateAllLiveUIs();
                }
                break;

            case CitiNetLiveMessageType.WatchStream:
                if (msg.Content == null) return;
                var watchNetEnt = NetEntity.Parse(msg.Content);
                var watchCam = GetEntity(watchNetEnt);
                if (EntityManager.EntityExists(watchCam))
                {
                    // Отписаться от предыдущего
                    if (ent.Comp.WatchedCamUid.HasValue)
                        _liveStream.RemoveViewer(ent.Comp.WatchedCamUid.Value, ownerUid.Value);

                    ent.Comp.WatchedCamUid = watchCam;
                    _liveStream.TryAddViewer(watchCam, ownerUid.Value);
                }
                UpdateUI(ent, GetEntity(args.LoaderUid));
                break;

            case CitiNetLiveMessageType.StopWatching:
                if (ent.Comp.WatchedCamUid.HasValue)
                {
                    _liveStream.RemoveViewer(ent.Comp.WatchedCamUid.Value, ownerUid.Value);
                    ent.Comp.WatchedCamUid = null;
                }
                UpdateUI(ent, GetEntity(args.LoaderUid));
                break;

            case CitiNetLiveMessageType.SendDonate:
                // Формат content: "amount|message"
                if (msg.Content == null || !ent.Comp.WatchedCamUid.HasValue) return;
                var parts = msg.Content.Split('|', 2);
                if (parts.Length != 2 || !int.TryParse(parts[0], out var amount)) return;
                _liveStream.SendDonation(ownerUid.Value, ent.Comp.WatchedCamUid.Value, amount, parts[1]);
                UpdateAllLiveUIs();
                break;

            case CitiNetLiveMessageType.SendChat:
                if (msg.Content == null) return;
                EntityUid? targetCam = ent.Comp.WatchedCamUid
                    ?? (cam.HasValue && TryComp<StreamCamComponent>(cam.Value, out var sc) && sc.IsStreaming
                        ? cam
                        : null);
                if (!targetCam.HasValue) return;
                if (TryComp<StreamCamComponent>(targetCam.Value, out var tcComp))
                {
                    var senderName = Name(ownerUid.Value);
                    _liveStream.AddChatMessage(tcComp,
                        new LiveChatMessage(_timing.CurTime, senderName, msg.Content, false));
                }
                UpdateAllLiveUIs();
                break;
        }
    }

    // ===================== UI =====================

    private void UpdateUI(Entity<CitiNetLiveCartridgeComponent> ent, EntityUid loader)
    {
        var ownerUid = GetPdaHolderUid(ent);

        var hasCam = false;
        var isStreaming = false;
        var streamTitle = string.Empty;
        var viewerCount = 0;
        var balance = 0;
        var batteryPercent = 0;
        var watchedCamNet = (NetEntity?) null;
        var watchedStreamerName = string.Empty;
        var chatMessages = new List<LiveChatMessage>();

        if (ownerUid != null)
        {
            var cam = FindStreamCam(ownerUid.Value);
            hasCam = cam.HasValue;

            if (cam.HasValue && TryComp<StreamCamComponent>(cam.Value, out var camComp))
            {
                camComp.HolderUid = ownerUid;
                isStreaming = camComp.IsStreaming;
                streamTitle = camComp.StreamTitle;
                viewerCount = camComp.ViewerCount;
                chatMessages = new List<LiveChatMessage>(camComp.ChatMessages);

                // Процент заряда батареи
                if (_powerCell.TryGetBatteryFromSlot(cam.Value, out var battery))
                    batteryPercent = (int) (battery.CurrentCharge / battery.MaxCharge * 100f);
            }

            balance = _bank.GetBalance(ownerUid.Value);
        }

        // Список всех активных стримов
        var activeStreams = new List<StreamInfo>();
        foreach (var streamCamUid in _liveStream.GetActiveStreams())
        {
            if (!TryComp<StreamCamComponent>(streamCamUid, out var sCam))
                continue;

            var streamerName = sCam.HolderUid.HasValue ? Name(sCam.HolderUid.Value) : Loc.GetString("citinet-live-unknown-streamer");
            activeStreams.Add(new StreamInfo(
                GetNetEntity(streamCamUid),
                sCam.StreamTitle,
                streamerName,
                sCam.ViewerCount));
        }

        // Что смотрит зритель
        if (ent.Comp.WatchedCamUid.HasValue)
        {
            watchedCamNet = GetNetEntity(ent.Comp.WatchedCamUid.Value);
            if (TryComp<StreamCamComponent>(ent.Comp.WatchedCamUid.Value, out var wCam))
            {
                watchedStreamerName = wCam.HolderUid.HasValue ? Name(wCam.HolderUid.Value) : Loc.GetString("citinet-live-unknown-streamer");
                chatMessages = new List<LiveChatMessage>(wCam.ChatMessages);
            }
        }

        var state = new CitiNetLiveUiState
        {
            HasCamera = hasCam,
            IsStreaming = isStreaming,
            ViewerCount = viewerCount,
            StreamTitle = streamTitle,
            Balance = balance,
            BatteryPercent = batteryPercent,
            ActiveStreams = activeStreams,
            WatchedCamNetEntity = watchedCamNet,
            WatchedStreamerName = watchedStreamerName,
            ChatMessages = chatMessages,
        };

        _cartridge.UpdateCartridgeUiState(loader, state);
    }

    // ===================== HELPERS =====================

    /// <summary>Рассылаем UI обновление всем Live-картриджам.</summary>
    public void UpdateAllLiveUIs()
    {
        var query = EntityQueryEnumerator<CitiNetLiveCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var cart))
        {
            if (cart.LoaderUid != null)
                UpdateUI((uid, comp), cart.LoaderUid.Value);
        }
    }

    /// <summary>Получить EntityUid моба, держащего PDA с этим картриджем.</summary>
    private EntityUid? GetPdaHolderUid(Entity<CitiNetLiveCartridgeComponent> ent)
    {
        if (!TryComp<CartridgeComponent>(ent, out var cart) || cart.LoaderUid == null)
            return null;

        var xform = Transform(cart.LoaderUid.Value);
        return xform.ParentUid;
    }

    /// <summary>Найти StreamCam в руках или инвентаре персонажа.</summary>
    private EntityUid? FindStreamCam(EntityUid person)
    {
        // Проверяем руки
        var handsQuery = EntityQueryEnumerator<StreamCamComponent, TransformComponent>();
        while (handsQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.ParentUid == person)
                return uid;
        }

        // Проверяем слоты инвентаря
        if (_inventory.TryGetContainerSlotEnumerator(person, out var slotEnum))
        {
            while (slotEnum.NextItem(out var item))
            {
                if (HasComp<StreamCamComponent>(item))
                    return item;
            }
        }

        return null;
    }
}
