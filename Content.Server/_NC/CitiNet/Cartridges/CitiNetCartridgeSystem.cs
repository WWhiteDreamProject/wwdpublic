using System.Linq;
using Content.Shared.Access.Components;
using Content.Server.CartridgeLoader;
using Content.Server.Power.Components;
using Content.Shared._NC.CitiNet;
using Content.Shared._NC.CitiNet.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.Hands.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.PDA;
using Robust.Server.GameObjects;
using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.Chat.Managers;

namespace Content.Server._NC.CitiNet.Cartridges;

/// <summary>
/// Серверная система картриджа CitiNet.
/// Обрабатывает P2P звонки, групповые звонки и BBS-каналы.
/// Маршрутизация через CitiNet Relay (требует питания).
/// </summary>
public sealed class CitiNetCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CitiNetCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<CitiNetCartridgeComponent, CartridgeMessageEvent>(OnMessage);
        SubscribeLocalEvent<CitiNetCartridgeComponent, CartridgeAddedEvent>(OnAdded);

        // Phase 2: FLATLINE — слушаем смерть/крит участников групповых звонков
        // Используем HandsComponent, т.к. MobStateComponent уже занят в SharedStunSystem
        SubscribeLocalEvent<HandsComponent, MobStateChangedEvent>(OnMobStateChanged);

        // Voice Relay
        SubscribeLocalEvent<EntitySpokeEvent>(OnSpeak);
    }

    // ========== Инициализация ==========

    /// <summary>
    /// При установке картриджа в PDA генерируем уникальный номер Агента.
    /// </summary>
    private void OnAdded(Entity<CitiNetCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        ent.Comp.LoaderUid = args.Loader;

        // Генерируем номер если ещё нет
        if (string.IsNullOrEmpty(ent.Comp.AgentNumber))
        {
            ent.Comp.AgentNumber = GenerateAgentNumber();
        }
    }

    private void OnUiReady(Entity<CitiNetCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        // Регистрируем как фоновую программу чтобы получать события даже когда не активна
        _cartridge.RegisterBackgroundProgram(args.Loader, ent);
        UpdateUI(ent, args.Loader);
    }

    // ========== Обработка UI-сообщений ==========

    private void OnMessage(Entity<CitiNetCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not CitiNetUiMessageEvent msg)
            return;

        var loader = GetEntity(args.LoaderUid);

        // Проверяем наличие CitiNet Relay для большинства операций
        switch (msg.Type)
        {
            // P2P чаты и звонки
            case CitiNetUiMessageType.StartChat:
                HandleStartChat(ent, msg);
                break;
            case CitiNetUiMessageType.CloseChat:
                HandleCloseChat(ent);
                break;
            case CitiNetUiMessageType.InitiateCall:
                HandleInitiateCall(ent, msg);
                break;
            case CitiNetUiMessageType.AcceptCall:
                HandleAcceptCall(ent);
                break;
            case CitiNetUiMessageType.DeclineCall:
                HandleDeclineCall(ent);
                break;
            case CitiNetUiMessageType.HangUp:
                HandleHangUp(ent);
                break;
            case CitiNetUiMessageType.SendCallMessage:
                HandleSendCallMessage(ent, msg);
                break;
            case CitiNetUiMessageType.PingLocation:
                HandlePingLocation(ent);
                break;

            // Групповые звонки
            case CitiNetUiMessageType.CreateGroup:
                HandleCreateGroup(ent);
                break;
            case CitiNetUiMessageType.InviteToGroup:
                HandleInviteToGroup(ent, msg);
                break;
            case CitiNetUiMessageType.LeaveGroup:
                HandleLeaveGroup(ent);
                break;
            case CitiNetUiMessageType.SendGroupMessage:
                HandleSendGroupMessage(ent, msg);
                break;
            case CitiNetUiMessageType.JoinGroupVoice:
                HandleJoinGroupVoice(ent);
                break;
            case CitiNetUiMessageType.LeaveGroupVoice:
                HandleLeaveGroupVoice(ent);
                break;

            // BBS
            case CitiNetUiMessageType.JoinChannel:
                HandleJoinChannel(ent, msg);
                break;
            case CitiNetUiMessageType.LeaveChannel:
                HandleLeaveChannel(ent, msg);
                break;
            case CitiNetUiMessageType.SendBBSMessage:
                HandleSendBBSMessage(ent, msg);
                break;
            case CitiNetUiMessageType.SelectChannel:
                HandleSelectChannel(ent, msg);
                break;
        }

        UpdateUI(ent, loader);
    }

    // ========== P2P Звонки и Чаты ==========

    private void HandleStartChat(Entity<CitiNetCartridgeComponent> ent, CitiNetUiMessageEvent msg)
    {
        if (msg.TargetId == null || !HasActiveCitiNetRelay(ent))
            return;

        // Ищем Агент с таким номером
        var target = FindCartridgeByNumber(msg.TargetId);
        if (target == null)
            return;

        if (target.Value.Owner == ent.Owner)
            return; // Нельзя открыть чат с самим собой

        if (ent.Comp.ActiveChatTarget != target.Value.Owner)
        {
            ent.Comp.ActiveChatTarget = target.Value.Owner;
            if (!ent.Comp.ChatHistories.ContainsKey(target.Value.Owner))
                ent.Comp.ChatHistories[target.Value.Owner] = new List<CitiNetCallMessage>();
        }

        UpdateUIForCartridge(ent);
    }

    private void HandleCloseChat(Entity<CitiNetCartridgeComponent> ent)
    {
        if (ent.Comp.ActiveChatTarget != null)
            ent.Comp.ChatHistories.Remove(ent.Comp.ActiveChatTarget.Value);

        ent.Comp.ActiveChatTarget = null;
        UpdateUIForCartridge(ent);
    }

    private void HandleInitiateCall(Entity<CitiNetCartridgeComponent> ent, CitiNetUiMessageEvent msg)
    {
        if (ent.Comp.ActiveChatTarget == null || ent.Comp.CallState != CitiNetCallState.None || !HasActiveCitiNetRelay(ent))
            return;

        var targetUid = ent.Comp.ActiveChatTarget.Value;
        if (!TryComp<CitiNetCartridgeComponent>(targetUid, out var targetComp))
            return;

        var target = new Entity<CitiNetCartridgeComponent>(targetUid, targetComp);
        if (!HasActiveCitiNetRelay(target))
            return;

        // Устанавливаем состояние "звоним"
        ent.Comp.CallState = CitiNetCallState.Ringing;

        // У цели — входящий вызов (автоматически открывает чат)
        target.Comp.CallState = CitiNetCallState.Incoming;
        target.Comp.IncomingCaller = ent.Owner;
        target.Comp.ActiveChatTarget = ent.Owner;

        // Обновляем UI цели и звонящего
        UpdateUIForCartridge(target);
        UpdateUIForCartridge(ent);
    }

    private void HandleAcceptCall(Entity<CitiNetCartridgeComponent> ent)
    {
        if (ent.Comp.CallState != CitiNetCallState.Incoming || ent.Comp.IncomingCaller == null)
            return;

        if (!TryComp<CitiNetCartridgeComponent>(ent.Comp.IncomingCaller, out var callerComp))
            return;

        // Устанавливаем активный звонок для обеих сторон
        ent.Comp.CallState = CitiNetCallState.Active;
        ent.Comp.ActiveChatTarget = ent.Comp.IncomingCaller;
        ent.Comp.IncomingCaller = null;

        callerComp.CallState = CitiNetCallState.Active;

        // Обновляем UI звонящего
        UpdateUIForCartridge((ent.Comp.ActiveChatTarget.Value, callerComp));
    }

    private void HandleDeclineCall(Entity<CitiNetCartridgeComponent> ent)
    {
        if (ent.Comp.CallState != CitiNetCallState.Incoming || ent.Comp.IncomingCaller == null)
            return;

        // Сбрасываем состояние у звонящего
        if (TryComp<CitiNetCartridgeComponent>(ent.Comp.IncomingCaller, out var callerComp))
        {
            callerComp.CallState = CitiNetCallState.None;
            UpdateUIForCartridge((ent.Comp.IncomingCaller.Value, callerComp));
        }

        ent.Comp.CallState = CitiNetCallState.None;
        ent.Comp.IncomingCaller = null;
    }

    private void HandleHangUp(Entity<CitiNetCartridgeComponent> ent)
    {
        var targetUid = ent.Comp.ActiveChatTarget ?? ent.Comp.IncomingCaller;
        if (targetUid != null && TryComp<CitiNetCartridgeComponent>(targetUid, out var targetComp))
        {
            if (targetComp.CallState != CitiNetCallState.None)
            {
                targetComp.CallState = CitiNetCallState.None;
                targetComp.IncomingCaller = null;
                UpdateUIForCartridge((targetUid.Value, targetComp));
            }
        }

        ent.Comp.CallState = CitiNetCallState.None;
        ent.Comp.IncomingCaller = null;
        UpdateUIForCartridge(ent);
    }

    private void HandleSendCallMessage(Entity<CitiNetCartridgeComponent> ent, CitiNetUiMessageEvent msg)
    {
        if (ent.Comp.ActiveChatTarget == null || string.IsNullOrWhiteSpace(msg.Content))
            return;

        if (!HasActiveCitiNetRelay(ent))
            return;

        var content = msg.Content.Trim();
        if (content.Length > CitiNetCallMessage.MaxContentLength)
            content = content[..CitiNetCallMessage.MaxContentLength];

        var senderName = GetOwnerName(ent);
        var message = new CitiNetCallMessage(_timing.CurTime, senderName, content);

        var targetUid = ent.Comp.ActiveChatTarget.Value;

        // Добавляем сообщение обеим сторонам
        AddP2PMessage(ent, targetUid, message);

        if (TryComp<CitiNetCartridgeComponent>(targetUid, out var targetComp))
        {
            var targetEnt = new Entity<CitiNetCartridgeComponent>(targetUid, targetComp);
            AddP2PMessage(targetEnt, ent.Owner, message);

            UpdateUIForCartridge(targetEnt);
        }

        // Обновляем UI отправителя
        UpdateUIForCartridge(ent);
    }

    private void HandlePingLocation(Entity<CitiNetCartridgeComponent> ent)
    {
        if (ent.Comp.ActiveChatTarget == null)
            return;

        // Phase 2: получаем реальные координаты PDA через TransformSystem
        var coords = GetPdaCoordinates(ent);
        var senderName = GetOwnerName(ent);
        var coordText = coords != null
            ? $"X:{coords.Value.X:F0} Y:{coords.Value.Y:F0}"
            : "[N/A]";

        var sysMsg = new CitiNetCallMessage(_timing.CurTime, "SYSTEM",
            Loc.GetString("citinet-call-ping-location", ("sender", senderName), ("coords", coordText)), true);

        var targetUid = ent.Comp.ActiveChatTarget.Value;
        AddP2PMessage(ent, targetUid, sysMsg);

        if (TryComp<CitiNetCartridgeComponent>(targetUid, out var targetComp))
        {
            var targetEnt = new Entity<CitiNetCartridgeComponent>(targetUid, targetComp);
            AddP2PMessage(targetEnt, ent.Owner, sysMsg);

            UpdateUIForCartridge(targetEnt);
        }

        UpdateUIForCartridge(ent);
    }

    private void AddP2PMessage(Entity<CitiNetCartridgeComponent> ent, EntityUid target, CitiNetCallMessage message)
    {
        if (!ent.Comp.ChatHistories.ContainsKey(target))
            ent.Comp.ChatHistories[target] = new List<CitiNetCallMessage>();

        ent.Comp.ChatHistories[target].Add(message);

        if (ent.Comp.ChatHistories[target].Count > ent.Comp.MaxMessagesPerChat)
            ent.Comp.ChatHistories[target].RemoveAt(0);
    }

    // ========== Phase 2: FLATLINE ==========

    /// <summary>
    /// При смерти/крите участника группового звонка — рассылаем FLATLINE всем в группе.
    /// </summary>
    private void OnMobStateChanged(EntityUid uid, HandsComponent hands, ref MobStateChangedEvent args)
    {
        // Только крит или смерть
        if (args.NewMobState != MobState.Critical && args.NewMobState != MobState.Dead)
            return;

        // Находим CitiNet картридж этого моба (через PDA в руках или на нём)
        var cartridge = FindCartridgeByOwner(uid);
        if (cartridge == null || !cartridge.Value.Comp.InGroup)
            return;

        var ownerName = GetOwnerName(cartridge.Value);
        var stateText = args.NewMobState == MobState.Dead
            ? Loc.GetString("citinet-flatline-dead", ("name", ownerName))
            : Loc.GetString("citinet-flatline-critical", ("name", ownerName));

        var flatlineMsg = new CitiNetCallMessage(_timing.CurTime, "FLATLINE", stateText, true);

        // Добавляем в общую историю группы
        cartridge.Value.Comp.GroupMessages.Add(flatlineMsg);

        // Обновляем статус участника (IsAlive = false)
        UpdateUIForGroup(cartridge.Value.Comp.GroupMembers);
    }

    // ========== Групповые звонки ==========

    private void HandleCreateGroup(Entity<CitiNetCartridgeComponent> ent)
    {
        if (ent.Comp.InGroup || !HasActiveCitiNetRelay(ent))
            return;

        ent.Comp.InGroup = true;
        ent.Comp.GroupMembers.Clear();
        ent.Comp.GroupMembers.Add(ent.Owner);
        ent.Comp.GroupMessages.Clear();

        UpdateUIForCartridge(ent);
    }

    private void HandleInviteToGroup(Entity<CitiNetCartridgeComponent> ent, CitiNetUiMessageEvent msg)
    {
        if (!ent.Comp.InGroup || msg.TargetId == null)
            return;

        if (ent.Comp.GroupMembers.Count >= ent.Comp.MaxGroupParticipants)
            return;

        var target = FindCartridgeByNumber(msg.TargetId);
        if (target == null || target.Value.Comp.InGroup)
            return;

        // Добавляем участника
        target.Value.Comp.InGroup = true;
        target.Value.Comp.GroupMembers = ent.Comp.GroupMembers; // Общая ссылка на группу
        ent.Comp.GroupMembers.Add(target.Value.Owner);
        target.Value.Comp.GroupMessages = ent.Comp.GroupMessages; // Общая история

        // Обновляем UI всех участников
        UpdateUIForGroup(ent.Comp.GroupMembers);
    }

    private void HandleLeaveGroup(Entity<CitiNetCartridgeComponent> ent)
    {
        if (!ent.Comp.InGroup)
            return;

        var members = ent.Comp.GroupMembers;
        members.Remove(ent.Owner);

        ent.Comp.InGroup = false;
        ent.Comp.GroupMembers = new HashSet<EntityUid>();
        ent.Comp.GroupMessages = new List<CitiNetCallMessage>();

        // Обновляем UI вышедшего игрока
        UpdateUIForCartridge(ent);

        // Если группа пуста, расформировываем
        if (members.Count <= 1)
        {
            foreach (var memberUid in members)
            {
                if (TryComp<CitiNetCartridgeComponent>(memberUid, out var memberComp))
                {
                    memberComp.InGroup = false;
                    memberComp.GroupMembers = new HashSet<EntityUid>();
                    memberComp.GroupMessages = new List<CitiNetCallMessage>();
                    UpdateUIForCartridge((memberUid, memberComp));
                }
            }
        }
        else
        {
            UpdateUIForGroup(members);
        }
    }

    private void HandleSendGroupMessage(Entity<CitiNetCartridgeComponent> ent, CitiNetUiMessageEvent msg)
    {
        if (!ent.Comp.InGroup || string.IsNullOrWhiteSpace(msg.Content))
            return;

        if (!HasActiveCitiNetRelay(ent))
            return;

        var content = msg.Content.Trim();
        if (content.Length > CitiNetCallMessage.MaxContentLength)
            content = content[..CitiNetCallMessage.MaxContentLength];

        var senderName = GetOwnerName(ent);
        var message = new CitiNetCallMessage(_timing.CurTime, senderName, content);

        // Добавляем в общую историю (shared reference)
        ent.Comp.GroupMessages.Add(message);

        // Обновляем UI всех участников
        UpdateUIForGroup(ent.Comp.GroupMembers);
    }

    private void HandleJoinGroupVoice(Entity<CitiNetCartridgeComponent> ent)
    {
        if (!ent.Comp.InGroup) return;
        ent.Comp.InGroupVoice = true;
        UpdateUIForGroup(ent.Comp.GroupMembers);
    }

    private void HandleLeaveGroupVoice(Entity<CitiNetCartridgeComponent> ent)
    {
        if (!ent.Comp.InGroup) return;
        ent.Comp.InGroupVoice = false;
        UpdateUIForGroup(ent.Comp.GroupMembers);
    }

    // ========== BBS-каналы ==========

    private void HandleJoinChannel(Entity<CitiNetCartridgeComponent> ent, CitiNetUiMessageEvent msg)
    {
        if (msg.TargetId == null)
            return;

        if (!_prototype.TryIndex<CitiNetBBSChannelPrototype>(msg.TargetId, out var channel))
            return;

        // Проверка доступа по ID-карте
        if (channel.Access != null && channel.Access.Count > 0)
        {
            HashSet<string> pdaAccess = new();
            if (ent.Comp.LoaderUid != null && TryComp<PdaComponent>(ent.Comp.LoaderUid.Value, out var pda) && pda.ContainedId != null)
            {
                if (TryComp<AccessComponent>(pda.ContainedId.Value, out var accessComp))
                {
                    pdaAccess = accessComp.Tags.Select(t => (string) t).ToHashSet();
                }
            }

            bool hasAccess = false;
            foreach (var requiredTag in channel.Access)
            {
                if (pdaAccess.Contains(requiredTag))
                {
                    hasAccess = true;
                    break;
                }
            }

            if (!hasAccess)
                return; // У персонажа нет нужного доступа
        }

        // Проверяем пароль если нужен
        if (channel.RequiresPassword)
        {
            if (string.IsNullOrEmpty(msg.Content) || msg.Content != channel.Password)
                return; // Неверный пароль
        }

        ent.Comp.JoinedChannels.Add(msg.TargetId);

        // If it's the first channel, select it automatically
        if (string.IsNullOrEmpty(ent.Comp.CurrentChannel))
            ent.Comp.CurrentChannel = msg.TargetId;

        // Инициализируем кеш сообщений для канала
        if (!ent.Comp.ChannelMessages.ContainsKey(msg.TargetId))
            ent.Comp.ChannelMessages[msg.TargetId] = new List<CitiNetBBSMessage>();
    }

    private void HandleLeaveChannel(Entity<CitiNetCartridgeComponent> ent, CitiNetUiMessageEvent msg)
    {
        if (msg.TargetId == null)
            return;

        ent.Comp.JoinedChannels.Remove(msg.TargetId);
        ent.Comp.ChannelMessages.Remove(msg.TargetId);

        if (ent.Comp.CurrentChannel == msg.TargetId)
            ent.Comp.CurrentChannel = ent.Comp.JoinedChannels.FirstOrDefault();
    }

    private void HandleSelectChannel(Entity<CitiNetCartridgeComponent> ent, CitiNetUiMessageEvent msg)
    {
        if (msg.TargetId != null && ent.Comp.JoinedChannels.Contains(msg.TargetId))
            ent.Comp.CurrentChannel = msg.TargetId;
    }

    private void HandleSendBBSMessage(Entity<CitiNetCartridgeComponent> ent, CitiNetUiMessageEvent msg)
    {
        if (ent.Comp.CurrentChannel == null || string.IsNullOrWhiteSpace(msg.Content))
            return;

        if (!HasActiveCitiNetRelay(ent))
            return;

        if (!_prototype.TryIndex<CitiNetBBSChannelPrototype>(ent.Comp.CurrentChannel, out var channel))
            return;

        var content = msg.Content.Trim();
        if (content.Length > CitiNetBBSMessage.MaxContentLength)
            content = content[..CitiNetBBSMessage.MaxContentLength];

        var senderName = channel.IsAnonymous
            ? Loc.GetString("citinet-bbs-anonymous")
            : GetOwnerName(ent);

        var message = new CitiNetBBSMessage(_timing.CurTime, senderName, content, ent.Comp.CurrentChannel);

        // Рассылаем всем подключённым к этому каналу
        var query = EntityQueryEnumerator<CitiNetCartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.JoinedChannels.Contains(ent.Comp.CurrentChannel))
                continue;

            // Проверяем CitiNet Relay для получателя
            if (!HasActiveCitiNetRelay((uid, comp)))
                continue;

            if (!comp.ChannelMessages.ContainsKey(ent.Comp.CurrentChannel))
                comp.ChannelMessages[ent.Comp.CurrentChannel] = new List<CitiNetBBSMessage>();

            comp.ChannelMessages[ent.Comp.CurrentChannel].Add(message);

            // Обрезаем историю
            if (comp.ChannelMessages[ent.Comp.CurrentChannel].Count > comp.MaxMessagesPerChannel)
                comp.ChannelMessages[ent.Comp.CurrentChannel].RemoveAt(0);

            // Обновляем UI получателя
            UpdateUIForCartridge((uid, comp));
        }
    }

    // ========== Утилиты ==========

    /// <summary>
    /// Проверяет наличие работающего CitiNet Relay на той же карте, что и PDA.
    /// </summary>
    private bool HasActiveCitiNetRelay(Entity<CitiNetCartridgeComponent> cartridge)
    {
        // Получаем PDA-загрузчик
        if (!TryComp<CartridgeComponent>(cartridge, out var cart) || cart.LoaderUid == null)
            return false;

        // Get actual map coordinates to account for containers (inventory, hands, etc)
        var mapCoords = _transform.GetMapCoordinates(cart.LoaderUid.Value);

        if (mapCoords.MapId == Robust.Shared.Map.MapId.Nullspace)
            return false;

        // Ищем активный CitiNet Relay на той же карте
        var query = EntityQueryEnumerator<CitiNetRelayComponent, ApcPowerReceiverComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var power, out var transform))
        {
            if (transform.MapID == mapCoords.MapId && power.Powered)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Ищет картридж CitiNet по номеру Агента.
    /// </summary>
    private Entity<CitiNetCartridgeComponent>? FindCartridgeByNumber(string number)
    {
        var query = EntityQueryEnumerator<CitiNetCartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AgentNumber == number)
                return (uid, comp);
        }
        return null;
    }

    /// <summary>
    /// Генерирует уникальный 4-значный номер Агента.
    /// </summary>
    private string GenerateAgentNumber()
    {
        // Собираем занятые номера
        var usedNumbers = new HashSet<string>();
        var query = EntityQueryEnumerator<CitiNetCartridgeComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            if (!string.IsNullOrEmpty(comp.AgentNumber))
                usedNumbers.Add(comp.AgentNumber);
        }

        // Генерируем уникальный
        string number;
        do
        {
            number = _random.Next(1000, 9999).ToString();
        } while (usedNumbers.Contains(number));

        return number;
    }

    /// <summary>
    /// Получает имя владельца PDA.
    /// </summary>
    private string GetOwnerName(Entity<CitiNetCartridgeComponent> ent)
    {
        if (!TryComp<CartridgeComponent>(ent, out var cart) || cart.LoaderUid == null)
            return ent.Comp.AgentNumber;

        if (TryComp<PdaComponent>(cart.LoaderUid, out var pda) && !string.IsNullOrEmpty(pda.OwnerName))
            return pda.OwnerName;

        return ent.Comp.AgentNumber;
    }

    /// <summary>
    /// Phase 2: Получает EntityUid моба, держащего PDA с этим картриджем.
    /// </summary>
    private EntityUid? GetPdaHolderUid(Entity<CitiNetCartridgeComponent> ent)
    {
        if (!TryComp<CartridgeComponent>(ent, out var cart) || cart.LoaderUid == null)
            return null;

        var xform = Transform(cart.LoaderUid.Value);
        return xform.ParentUid;
    }

    /// <summary>
    /// Phase 2: Получает координаты PDA на карте.
    /// </summary>
    private Vector2? GetPdaCoordinates(Entity<CitiNetCartridgeComponent> ent)
    {
        if (!TryComp<CartridgeComponent>(ent, out var cart) || cart.LoaderUid == null)
            return null;

        var worldPos = _transform.GetWorldPosition(cart.LoaderUid.Value);
        return worldPos;
    }

    /// <summary>
    /// Phase 2: Ищет картридж CitiNet по владельцу (EntityUid моба).
    /// Проверяет PDA через HandsComponent → перебираем все PDA в руках.
    /// </summary>
    private Entity<CitiNetCartridgeComponent>? FindCartridgeByOwner(EntityUid ownerUid)
    {
        if (!TryComp<HandsComponent>(ownerUid, out var hands))
            return null;

        // Перебираем все предметы в руках
        foreach (var hand in hands.Hands.Values)
        {
            if (hand.HeldEntity == null)
                continue;

            // Проверяем является ли предмет PDA с CartridgeLoader
            if (!TryComp<CartridgeLoaderComponent>(hand.HeldEntity, out var loader))
                continue;

            // Ищем CitiNet картридж среди активных/фоновых программ
            if (loader.ActiveProgram != null && TryComp<CitiNetCartridgeComponent>(loader.ActiveProgram, out var activeCitinet))
                return (loader.ActiveProgram.Value, activeCitinet);

            foreach (var programUid in loader.BackgroundPrograms)
            {
                if (TryComp<CitiNetCartridgeComponent>(programUid, out var citinet))
                    return (programUid, citinet);
            }
        }

        // Phase 2b: Ищем PDA в инвентаре (карманы/пояс) если нет в руках
        if (TryComp<Content.Shared.Inventory.InventoryComponent>(ownerUid, out var inv))
        {
            var invSystem = EntityManager.System<Content.Shared.Inventory.InventorySystem>();
            if (invSystem.TryGetSlotEntity(ownerUid, "id", out var idPda))
            {
                if (TryComp<CartridgeLoaderComponent>(idPda, out var loader))
                {
                    if (loader.ActiveProgram != null && TryComp<CitiNetCartridgeComponent>(loader.ActiveProgram, out var activeCitinet))
                        return (loader.ActiveProgram.Value, activeCitinet);

                    foreach (var programUid in loader.BackgroundPrograms)
                    {
                        if (TryComp<CitiNetCartridgeComponent>(programUid, out var citinet))
                            return (programUid, citinet);
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Перехват локальной речи для ретрансляции через открытые звонки (Voice Relay).
    /// </summary>
    private void OnSpeak(EntitySpokeEvent args)
    {
        if (args.Channel != null || args.IsWhisper || string.IsNullOrWhiteSpace(args.Message))
            return;

        // Только живые игроки
        if (!HasComp<MobStateComponent>(args.Source))
            return;

        var cartTuple = FindCartridgeByOwner(args.Source);
        if (cartTuple == null)
            return;

        var ent = cartTuple.Value;
        if (!HasActiveCitiNetRelay(ent))
            return;

        var message = args.Message;
        var sourceName = GetOwnerName(ent);

        // P2P звонок
        if (ent.Comp.CallState == CitiNetCallState.Active && ent.Comp.ActiveChatTarget != null)
        {
            var targetUid = ent.Comp.ActiveChatTarget.Value;
            if (TryComp<CitiNetCartridgeComponent>(targetUid, out var targetComp))
            {
                var target = new Entity<CitiNetCartridgeComponent>(targetUid, targetComp);
                if (HasActiveCitiNetRelay(target))
                {
                    if (TryComp<CartridgeComponent>(target.Owner, out var targetCart) && targetCart.LoaderUid != null)
                    {
                        _chat.TrySendInGameICMessage(targetCart.LoaderUid.Value, message, InGameICChatType.Speak, ChatTransmitRange.Normal, nameOverride: sourceName, checkRadioPrefix: false, ignoreActionBlocker: true, languageOverride: args.Language);
                    }
                }
            }
        }
        // Групповой звонок
        else if (ent.Comp.InGroup && ent.Comp.InGroupVoice)
        {
            foreach (var memberUid in ent.Comp.GroupMembers)
            {
                if (memberUid == ent.Owner)
                    continue;

                if (TryComp<CartridgeComponent>(memberUid, out var targetCart) && targetCart.LoaderUid != null)
                {
                    if (TryComp<CitiNetCartridgeComponent>(memberUid, out var memberComp) && memberComp.InGroupVoice && HasActiveCitiNetRelay((memberUid, memberComp)))
                    {
                        _chat.TrySendInGameICMessage(targetCart.LoaderUid.Value, message, InGameICChatType.Speak, ChatTransmitRange.Normal, nameOverride: sourceName, checkRadioPrefix: false, ignoreActionBlocker: true, languageOverride: args.Language);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Обновляет UI для конкретного картриджа.
    /// </summary>
    private void UpdateUIForCartridge(Entity<CitiNetCartridgeComponent> ent)
    {
        if (!TryComp<CartridgeComponent>(ent, out var cart) || cart.LoaderUid == null)
            return;

        UpdateUI(ent, cart.LoaderUid.Value);
    }

    /// <summary>
    /// Обновляет UI для всех участников группы.
    /// </summary>
    private void UpdateUIForGroup(HashSet<EntityUid> members)
    {
        foreach (var memberUid in members)
        {
            if (TryComp<CitiNetCartridgeComponent>(memberUid, out var comp))
                UpdateUIForCartridge((memberUid, comp));
        }
    }

    /// <summary>
    /// Собирает и отправляет полное состояние UI.
    /// </summary>
    private void UpdateUI(Entity<CitiNetCartridgeComponent> ent, EntityUid loader)
    {
        var hasRelay = HasActiveCitiNetRelay(ent);

        // Собираем список контактов
        var contacts = new List<CitiNetContact>();
        foreach (var uid in ent.Comp.ChatHistories.Keys)
        {
            if (TryComp<CitiNetCartridgeComponent>(uid, out var contactComp))
            {
                var name = GetOwnerName((uid, contactComp));
                contacts.Add(new CitiNetContact(contactComp.AgentNumber, name));
            }
        }

        string? currentContactNumber = null;
        var currentMessages = new List<CitiNetCallMessage>();

        if (ent.Comp.ActiveChatTarget != null && TryComp<CitiNetCartridgeComponent>(ent.Comp.ActiveChatTarget, out var target))
        {
            currentContactNumber = target.AgentNumber;
            if (ent.Comp.ChatHistories.TryGetValue(ent.Comp.ActiveChatTarget.Value, out var p2pMsgs))
                currentMessages = p2pMsgs;
        }
        else if (ent.Comp.IncomingCaller != null && TryComp<CitiNetCartridgeComponent>(ent.Comp.IncomingCaller, out var caller))
        {
            currentContactNumber = caller.AgentNumber;
            if (ent.Comp.ChatHistories.TryGetValue(ent.Comp.IncomingCaller.Value, out var p2pMsgs))
                currentMessages = p2pMsgs;
        }

        // Участники группы + Phase 2: проверяем IsAlive через MobState
        var groupParticipants = new List<CitiNetGroupParticipant>();
        foreach (var memberUid in ent.Comp.GroupMembers)
        {
            if (!TryComp<CitiNetCartridgeComponent>(memberUid, out var memberComp))
                continue;

            var name = GetOwnerName((memberUid, memberComp));
            var isAlive = true;

            // Проверяем MobState владельца PDA
            var ownerUid = GetPdaHolderUid((memberUid, memberComp));
            if (ownerUid != null && TryComp<MobStateComponent>(ownerUid, out var mobState))
                isAlive = mobState.CurrentState == MobState.Alive;

            groupParticipants.Add(new CitiNetGroupParticipant(name, isAlive));
        }

        // Собираем доступы (теги) из ID-карты в PDA
        HashSet<string> pdaAccess = new();
        if (ent.Comp.LoaderUid != null && TryComp<PdaComponent>(ent.Comp.LoaderUid.Value, out var pda) && pda.ContainedId != null)
        {
            if (TryComp<AccessComponent>(pda.ContainedId.Value, out var accessComp))
            {
                pdaAccess = accessComp.Tags.Select(t => (string) t).ToHashSet();
            }
        }

        // Список BBS-каналов (только публичные, по доступу, или те, к которым мы уже присоединились)
        var channels = new List<CitiNetChannelInfo>();
        foreach (var proto in _prototype.EnumeratePrototypes<CitiNetBBSChannelPrototype>())
        {
            var isJoined = ent.Comp.JoinedChannels.Contains(proto.ID);

            // Проверка доступа по ID
            bool hasAccess = true;
            if (proto.Access != null && proto.Access.Count > 0)
            {
                hasAccess = false;
                foreach (var requiredTag in proto.Access)
                {
                    if (pdaAccess.Contains(requiredTag))
                    {
                        hasAccess = true;
                        break;
                    }
                }
            }

            // Если доступа нет, канал даже не показывается в списке
            if (!hasAccess)
                continue;

            // Показываем только если мы присоединены, либо если канал не скрыт
            if (isJoined || !proto.IsHidden)
            {
                channels.Add(new CitiNetChannelInfo(
                    proto.ID,
                    proto.LocalizedName,
                    proto.Color,
                    proto.RequiresPassword,
                    isJoined));
            }
        }

        // Сообщения текущего BBS-канала
        var channelMessages = new List<CitiNetBBSMessage>();
        if (ent.Comp.CurrentChannel != null &&
            ent.Comp.ChannelMessages.TryGetValue(ent.Comp.CurrentChannel, out var msgs))
        {
            channelMessages = msgs;
        }

        var state = new CitiNetUiState(
            ent.Comp.AgentNumber,
            hasRelay,
            contacts,
            currentContactNumber,
            ent.Comp.CallState,
            currentMessages,
            ent.Comp.InGroup,
            ent.Comp.InGroupVoice,
            groupParticipants,
            ent.Comp.MaxGroupParticipants,
            ent.Comp.GroupMessages,
            channels,
            ent.Comp.CurrentChannel,
            channelMessages);

        _cartridge.UpdateCartridgeUiState(loader, state);
    }
}
