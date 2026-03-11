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

    private void OnAfterInteract(EntityUid uid, CitationDeviceComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        if (!HasComp<MindContainerComponent>(args.Target.Value) && !HasComp<ActorComponent>(args.Target.Value))
            return;

        if (args.User == args.Target.Value)
        {
            _popupSystem.PopupEntity("Нельзя оштрафовать самого себя", uid, args.User);
            return;
        }

        var stationUid = GetStation(uid, args.User);
        if (stationUid != null && _ncpdSystem.IsSuspended(stationUid.Value, args.User))
        {
            _popupSystem.PopupEntity("УЧЕТНАЯ ЗАПИСЬ ПРИОСТАНОВЛЕНА", uid, args.User);
            return;
        }

        if (CheckCooldown(args.Target.Value, out var timeLeft))
        {
            _popupSystem.PopupEntity($"ЦЕЛЬ УЖЕ ОШТРАФОВАНА НЕДАВНО. Ждите {timeLeft.Minutes}м {timeLeft.Seconds}с", uid, args.User);
            return;
        }

        component.ActiveTarget = args.Target.Value;
        component.ActiveIdCard = null;
        component.ActiveOfficer = args.User;
        OpenCopUI(uid, component, args.User);
        args.Handled = true;
    }

    private void OnInteractUsing(EntityUid uid, CitationDeviceComponent component, InteractUsingEvent args)
    {
        if (!TryComp<IdCardComponent>(args.Used, out var idCard))
            return;

        var stationUid = GetStation(uid, args.User);
        if (stationUid != null && _ncpdSystem.IsSuspended(stationUid.Value, args.User))
        {
            _popupSystem.PopupEntity("УЧЕТНАЯ ЗАПИСЬ ПРИОСТАНОВЛЕНА", uid, args.User);
            return;
        }

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

        int budget = GetNcpdBudget(uid, user);

        var state = new CitationDeviceBuiState(targetName, limit, budget, false);
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
            "Captain" => 99999,
            "HoS" => component.DetectiveLimit,
            "Warden" => component.DetectiveLimit,
            "Detective" => component.DetectiveLimit,
            _ => component.PatrolLimit
        };
    }

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

        if (component.ActiveTarget != null)
        {
            if (HasComp<StunnedComponent>(component.ActiveTarget.Value) || HasComp<KnockedDownComponent>(component.ActiveTarget.Value))
            {
                _popupSystem.PopupEntity("Подозреваемый без сознания! Снимите ID-карту", uid, user);
                return;
            }

            var ev = new CitationTargetUiMessage(GetNetEntity(uid), Name(user), amount, args.Reason);
            RaiseNetworkEvent(ev, component.ActiveTarget.Value);

            int budget = GetNcpdBudget(uid, user);
            var state = new CitationDeviceBuiState(Name(component.ActiveTarget.Value), limit, budget, true);
            _uiSystem.SetUiState(uid, CitationDeviceUiKey.Key, state);
            
            _popupSystem.PopupEntity("Запрос отправлен", uid, user);
        }
    }

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
            if (cop != null)
                _popupSystem.PopupEntity("ОТКАЗ В ОПЛАТЕ", uid, cop.Value);
            
            _popupSystem.PopupEntity("Вы отказались от штрафа", target, target);
            ResetTerminal(uid, component);
            return;
        }

        ProcessPayment(uid, component, target, component.RequestedAmount);
    }

    private void OnForceDoAfter(EntityUid uid, CitationDeviceComponent component, CitationForceDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var targetCard = args.Args.Target;
        if (targetCard == null) return;

        if (!TryComp<IdCardComponent>(targetCard.Value, out var idCard) || idCard.FullName == null)
        {
            _popupSystem.PopupEntity("Не удалось установить владельца карты", uid, args.Args.User);
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
            _popupSystem.PopupEntity("Владелец карты не найден на сервере", uid, args.Args.User);
            return;
        }

        _popupSystem.PopupEntity("Банковский пин-код взломан", uid, args.Args.User);
        ProcessPayment(uid, component, cardOwner.Value, args.Amount, true);
        
        args.Handled = true;
    }

    private void ProcessPayment(EntityUid terminalUid, CitationDeviceComponent component, EntityUid targetUid, int amount, bool isForced = false)
    {
        var cop = component.ActiveOfficer;
        if (cop == null) return;

        if (!_bankSystem.TryBankWithdraw(targetUid, amount))
        {
            _popupSystem.PopupEntity("НЕДОСТАТОЧНО СРЕДСТВ", terminalUid, cop.Value);
            if (cop != targetUid)
                _popupSystem.PopupEntity("Недостаточно средств на счете", targetUid, targetUid);
            ResetTerminal(terminalUid, component);
            return;
        }

        int copShare = (int)(amount * component.CommissionRate);
        int deptShare = amount - copShare;

        _bankSystem.TryBankDeposit(cop.Value, copShare);

        var stationUid = GetStation(terminalUid, cop.Value);

        string jobName = "Офицер";
        if (_idCardSystem.TryFindIdCard(cop.Value, out var idCard) && !string.IsNullOrWhiteSpace(idCard.Comp.LocalizedJobTitle))
        {
            jobName = idCard.Comp.LocalizedJobTitle;
        }
        string fullCopName = $"{jobName} {Name(cop.Value)}";

        if (stationUid != null)
        {
            _bankSystem.TryFactionDeposit(stationUid.Value, SectorBankAccount.Ncpd, deptShare);
            _ncpdSystem.AddLog(stationUid.Value, fullCopName, Name(targetUid), amount, isForced ? "ПРИНУДИТЕЛЬНОЕ СПИСАНИЕ" : $"ДОЛЯ NCPD: +{deptShare} эдди", component.Reason);
        }

        PrintCitationPaper(terminalUid, fullCopName, Name(targetUid), amount, component.Reason);

        _popupSystem.PopupEntity($"ОПЛАТА УСПЕШНА: +{copShare} Эдди комиссии", terminalUid, cop.Value);
        _popupSystem.PopupEntity($"Вы оплатили штраф: {amount} Эдди", targetUid, targetUid);

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
        var officer = component.ActiveOfficer;
        component.ActiveTarget = null;
        component.ActiveIdCard = null;
        component.RequestedAmount = 0;
        component.Reason = string.Empty;
        
        if (officer != null)
        {
            _uiSystem.CloseUi(uid, CitationDeviceUiKey.Key, officer.Value);
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

    private EntityUid? GetStation(EntityUid terminal, EntityUid? user)
    {
        var station = _stationSystem.GetOwningStation(terminal);
        if (station == null && user != null)
            station = _stationSystem.GetOwningStation(user.Value);
        
        if (station == null)
            station = _stationSystem.GetStationsSet().FirstOrDefault();

        return station;
    }

    private int GetNcpdBudget(EntityUid terminal, EntityUid user)
    {
        var stationUid = GetStation(terminal, user);
        if (stationUid == null) return 0;

        var bank = _bankSystem.EnsureStationBank(stationUid.Value);
        if (bank.Accounts.TryGetValue(SectorBankAccount.Ncpd, out var account))
        {
            return account.Balance;
        }
        return 0;
    }
}
