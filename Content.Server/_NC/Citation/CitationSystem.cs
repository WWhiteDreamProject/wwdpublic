using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Roles.Jobs;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Server._NC.Bank;
using Content.Server._NC.Ncpd;
using Content.Shared._NC.Bank;
using Content.Shared._NC.Citation;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Player;
using Content.Shared.Stunnable;
using Content.Shared.Cuffs.Components;
using Content.Shared.Access.Components;
using Robust.Shared.Containers;
using System;
using System.Linq;

namespace Content.Server._NC.Citation;

/// <summary>
/// Система, отвечающая за логику терминала штрафов (NCPD Citation Terminal).
/// </summary>
public sealed class CitationSystem : EntitySystem
{
    [Dependency] private readonly BankSystem _bankSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedJobSystem _jobSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly NcpdSystem _ncpdSystem = default!;
    [Dependency] private readonly Content.Shared.Access.Systems.SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly Content.Shared.Paper.PaperSystem _paperSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CitationDeviceComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CitationDeviceComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CitationDeviceComponent, CitationDeviceCreateMessage>(OnUiMessage);
        
        SubscribeNetworkEvent<CitationTargetResponseMessage>(OnTargetResponse);
        
        SubscribeLocalEvent<CitationDeviceComponent, CitationForceDoAfterEvent>(OnForceDoAfter);
    }

    /// <summary>
    /// Офицер кликает терминалом по другому игроку.
    /// </summary>
    private void OnAfterInteract(EntityUid uid, CitationDeviceComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        // Терминал применяется на игрока
        if (!HasComp<MindContainerComponent>(args.Target.Value) && !HasComp<ActorComponent>(args.Target.Value))
            return;

        if (args.User == args.Target.Value)
        {
            _popupSystem.PopupEntity("Нельзя оштрафовать самого себя.", uid, args.User);
            return;
        }

        var stationUid = _stationSystem.GetOwningStation(uid) ?? _stationSystem.GetOwningStation(args.User);
        if (stationUid != null && _ncpdSystem.IsSuspended(stationUid.Value, args.User))
        {
            _popupSystem.PopupEntity("УЧЕТНАЯ ЗАПИСЬ ПРИОСТАНОВЛЕНА", uid, args.User);
            return;
        }

        // Проверяем кулдаун на цель
        if (CheckCooldown(args.Target.Value, out var timeLeft))
        {
            _popupSystem.PopupEntity($"ЦЕЛЬ УЖЕ ОШТРАФОВАНА НЕДАВНО. Ждите {timeLeft.Minutes}м {timeLeft.Seconds}с", uid, args.User);
            return;
        }

        // Открываем UI копу
        component.ActiveTarget = args.Target.Value;
        component.ActiveIdCard = null;
        component.ActiveOfficer = args.User;
        OpenCopUI(uid, component, args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Офицер кликает чужой ID-картой по терминалу (для принудительного штрафа).
    /// </summary>
    private void OnInteractUsing(EntityUid uid, CitationDeviceComponent component, InteractUsingEvent args)
    {
        if (!TryComp<IdCardComponent>(args.Used, out var idCard))
            return;

        var stationUid = _stationSystem.GetOwningStation(uid) ?? _stationSystem.GetOwningStation(args.User);
        if (stationUid != null && _ncpdSystem.IsSuspended(stationUid.Value, args.User))
        {
            _popupSystem.PopupEntity("УЧЕТНАЯ ЗАПИСЬ ПРИОСТАНОВЛЕНА", uid, args.User);
            return;
        }

        // Открываем UI копу, запоминаем карту
        component.ActiveIdCard = args.Used;
        component.ActiveTarget = null;
        component.ActiveOfficer = args.User;
        OpenCopUI(uid, component, args.User);
        args.Handled = true;
    }

    private void OpenCopUI(EntityUid uid, CitationDeviceComponent component, EntityUid user)
    {
        _uiSystem.OpenUi(uid, CitationDeviceUiKey.Key, user);

        int limit = GetCitationLimit(user, component);
        string targetName = "Неизвестный";
        
        if (component.ActiveTarget != null)
            targetName = Name(component.ActiveTarget.Value);
        else if (component.ActiveIdCard != null)
            targetName = CompOrNull<IdCardComponent>(component.ActiveIdCard.Value)?.FullName ?? "Неизвестная карта";

        var state = new CitationDeviceBuiState(targetName, limit, false);
        _uiSystem.SetUiState(uid, CitationDeviceUiKey.Key, state);
    }

    private int GetCitationLimit(EntityUid user, CitationDeviceComponent component)
    {
        if (!_mindSystem.TryGetMind(user, out var mindId, out _))
            return component.PatrolLimit;

        if (!_jobSystem.MindTryGetJobId(mindId, out var jobId))
            return component.PatrolLimit;

        return (jobId?.Id ?? "Unknown") switch
        {
            "Captain" => 99999, // Безлимит
            "HoS" => component.DetectiveLimit,
            "Warden" => component.DetectiveLimit,
            "Detective" => component.DetectiveLimit,
            _ => component.PatrolLimit
        };
    }

    /// <summary>
    /// Офицер ввел сумму и нажал "Выписать штраф".
    /// </summary>
    private void OnUiMessage(EntityUid uid, CitationDeviceComponent component, CitationDeviceCreateMessage args)
    {
        var user = args.Actor;

        if (user != component.ActiveOfficer)
            return;

        int limit = GetCitationLimit(user, component);
        int amount = Math.Min(args.Amount, limit);
        if (amount <= 0) amount = 50;

        component.RequestedAmount = amount;
        component.Reason = args.Reason;

        // Если это принудительный штраф через ID-карту
        if (component.ActiveIdCard != null)
        {
            var doAfterArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(5), new CitationForceDoAfterEvent(amount), uid, target: component.ActiveIdCard.Value)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = true
            };
            
            _doAfterSystem.TryStartDoAfter(doAfterArgs);
            _uiSystem.CloseUi(uid, CitationDeviceUiKey.Key, user);
            return;
        }

        // Если это добровольный штраф по игроку
        if (component.ActiveTarget != null)
        {
            // Проверка, может ли цель вообще ответить (в сознании ли)
            if (HasComp<StunnedComponent>(component.ActiveTarget.Value) || HasComp<KnockedDownComponent>(component.ActiveTarget.Value))
            {
                _popupSystem.PopupEntity("Подозреваемый без сознания! Снимите с него ID-карту для принудительного штрафа.", uid, user);
                return;
            }

            // Отправляем Alert-диалог подозреваемому
            var ev = new CitationTargetUiMessage(GetNetEntity(uid), Name(user), amount, args.Reason);
            RaiseNetworkEvent(ev, component.ActiveTarget.Value);

            // Обновляем UI копу (Ожидание)
            var state = new CitationDeviceBuiState(Name(component.ActiveTarget.Value), limit, true);
            _uiSystem.SetUiState(uid, CitationDeviceUiKey.Key, state);
            
            _popupSystem.PopupEntity("Запрос отправлен...", uid, user);
        }
    }

    /// <summary>
    /// Ответ от подозреваемого (Оплатить / Отказаться).
    /// </summary>
    private void OnTargetResponse(CitationTargetResponseMessage ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } target)
            return;

        var uid = GetEntity(ev.TerminalUid);
        if (!TryComp<CitationDeviceComponent>(uid, out var component))
            return;

        if (component.ActiveTarget != target)
            return;

        var cop = component.ActiveOfficer;

        if (!ev.Accept)
        {
            // ОТКАЗ
            if (cop != null)
                _popupSystem.PopupEntity("ОТКАЗ В ОПЛАТЕ!", uid, cop.Value);
            
            _popupSystem.PopupEntity("Вы отказались от штрафа.", target, target);
            ResetTerminal(uid, component);
            return;
        }

        // ОПЛАТА
        ProcessPayment(uid, component, target, component.RequestedAmount);
    }

    /// <summary>
    /// Завершение DoAfter для принудительного штрафа по ID карте.
    /// </summary>
    private void OnForceDoAfter(EntityUid uid, CitationDeviceComponent component, CitationForceDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var targetCard = args.Args.Target;
        if (targetCard == null) return;

        // Поиск владельца по ID-карте
        if (!TryComp<IdCardComponent>(targetCard.Value, out var idCard) || idCard.FullName == null)
        {
            _popupSystem.PopupEntity("Не удалось установить владельца карты.", uid, args.Args.User);
            return;
        }

        EntityUid? cardOwner = null;
        var query = EntityQueryEnumerator<ActorComponent, MindContainerComponent>();
        while (query.MoveNext(out var playerUid, out var actor, out var mindContainer))
        {
            if (Name(playerUid) == idCard.FullName)
            {
                cardOwner = playerUid;
                break;
            }
        }

        if (cardOwner == null)
        {
            _popupSystem.PopupEntity("Владелец карты не найден на сервере (возможно, отключился).", uid, args.Args.User);
            return;
        }

        // Принудительная оплата
        _popupSystem.PopupEntity("Банковский пин-код взломан!", uid, args.Args.User);
        ProcessPayment(uid, component, cardOwner.Value, args.Amount, true);
        
        args.Handled = true;
    }

    private void ProcessPayment(EntityUid terminalUid, CitationDeviceComponent component, EntityUid targetUid, int amount, bool isForced = false)
    {
        var cop = component.ActiveOfficer;
        if (cop == null) return;

        // Пытаемся снять деньги
        if (!_bankSystem.TryBankWithdraw(targetUid, amount))
        {
            _popupSystem.PopupEntity("НЕДОСТАТОЧНО СРЕДСТВ!", terminalUid, cop.Value);
            if (cop != targetUid)
                _popupSystem.PopupEntity("Недостаточно средств на счете.", targetUid, targetUid);
            ResetTerminal(terminalUid, component);
            return;
        }

        // Деньги сняты. Распределяем: 30% копу, 70% департаменту
        int copShare = (int)(amount * component.CommissionRate);
        int deptShare = amount - copShare;

        _bankSystem.TryBankDeposit(cop.Value, copShare);

        var stationUid = _stationSystem.GetOwningStation(terminalUid) ?? _stationSystem.GetOwningStation(cop.Value);
        if (stationUid == null && _stationSystem.GetStationsSet().Count > 0)
        {
            stationUid = _stationSystem.GetStationsSet().First();
        }

        string jobName = "Офицер";
        if (_idCardSystem.TryFindIdCard(cop.Value, out var idCard) && !string.IsNullOrWhiteSpace(idCard.Comp.LocalizedJobTitle))
        {
            jobName = idCard.Comp.LocalizedJobTitle;
        }
        string fullCopName = $"{jobName} {Name(cop.Value)}";

        if (stationUid != null)
        {
            _bankSystem.TryFactionDeposit(stationUid.Value, SectorBankAccount.Ncpd, deptShare);
            
            // Логируем
            string status = isForced ? "ПРИНУДИТЕЛЬНОЕ СПИСАНИЕ" : $"ДОЛЯ NCPD: +{deptShare} эдди";
            _ncpdSystem.AddLog(stationUid.Value, fullCopName, Name(targetUid), amount, status, component.Reason);
        }

        // ПЕЧАТЬ БУМАГИ
        PrintCitationPaper(terminalUid, fullCopName, Name(targetUid), amount, component.Reason);

        // Успех
        _popupSystem.PopupEntity($"ОПЛАТА УСПЕШНА: +{copShare} Эдди комиссии.", terminalUid, cop.Value);
        _popupSystem.PopupEntity($"Вы оплатили штраф: {amount} Эдди.", targetUid, targetUid);

        // Ставим кулдаун
        EnsureComp<CitationCooldownComponent>(targetUid).LastCitationTime = _timing.CurTime;

        ResetTerminal(terminalUid, component);
    }

    private void PrintCitationPaper(EntityUid terminalUid, string officer, string suspect, int amount, string reason)
    {
        var coords = Transform(terminalUid).Coordinates;
        var paper = Spawn("Paper", coords);
        
        if (TryComp<Content.Shared.Paper.PaperComponent>(paper, out var paperComp))
        {
            string content = $"--- КВИТАНЦИЯ NCPD ---\n\n" +
                             $"ОФИЦЕР: {officer}\n" +
                             $"НАРУШИТЕЛЬ: {suspect}\n" +
                             $"СУММА: {amount} Эдди\n" +
                             $"ПРИЧИНА: {reason}\n\n" +
                             $"СТАТУС: ОПЛАЧЕНО\n" +
                             $"----------------------";
            
            _paperSystem.SetContent((paper, paperComp), content);
            
            // Ставим печать NCPD (условную)
            var stamp = new Content.Shared.Paper.StampDisplayInfo
            {
                StampedName = "NCPD BUREAU",
                StampedColor = Color.Yellow
            };
            _paperSystem.TryStamp((paper, paperComp), stamp, "stamp_police");
        }
    }

    private void ResetTerminal(EntityUid uid, CitationDeviceComponent component)
    {
        component.ActiveTarget = null;
        component.ActiveIdCard = null;
        component.RequestedAmount = 0;
        component.Reason = string.Empty;
        
        // Find the actor to close UI for
        if (component.ActiveOfficer != null)
        {
            _uiSystem.CloseUi(uid, CitationDeviceUiKey.Key, component.ActiveOfficer.Value);
        }
    }

    private bool CheckCooldown(EntityUid target, out TimeSpan timeLeft)
    {
        timeLeft = TimeSpan.Zero;
        if (TryComp<CitationCooldownComponent>(target, out var cooldown))
        {
            var elapsed = _timing.CurTime - cooldown.LastCitationTime;
            if (elapsed < cooldown.CooldownDuration)
            {
                timeLeft = cooldown.CooldownDuration - elapsed;
                return true;
            }
        }
        return false;
    }
}