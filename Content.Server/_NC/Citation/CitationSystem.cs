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
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.Cuffs.Components;
using Content.Shared.Access.Components;
using Robust.Shared.Containers;
using System;
using System.Linq;
using Robust.Shared.Localization;

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
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

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
            _popupSystem.PopupEntity(Loc.GetString("citation-popup-self"), uid, args.User);
            return;
        }

        var stationUid = GetStation(uid, args.User);
        if (stationUid != null && _ncpdSystem.IsSuspended(stationUid.Value, args.User))
        {
            _popupSystem.PopupEntity(Loc.GetString("citation-popup-suspended"), uid, args.User);
            return;
        }

        if (CheckCooldown(args.Target.Value, out var timeLeft))
        {
            _popupSystem.PopupEntity(Loc.GetString("citation-popup-cooldown", ("minutes", timeLeft.Minutes), ("seconds", timeLeft.Seconds)), uid, args.User);
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
            _popupSystem.PopupEntity(Loc.GetString("citation-popup-suspended"), uid, args.User);
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
        string targetName = Loc.GetString("citation-ui-unknown");
        
        if (component.ActiveTarget != null)
            targetName = Name(component.ActiveTarget.Value);
        else if (component.ActiveIdCard != null)
            targetName = CompOrNull<IdCardComponent>(component.ActiveIdCard.Value)?.FullName ?? Loc.GetString("citation-ui-unknown-card");

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
                _popupSystem.PopupEntity(Loc.GetString("citation-popup-unconscious"), uid, user);
                return;
            }

            var ev = new CitationTargetUiMessage(GetNetEntity(uid), Name(user), amount, args.Reason);
            RaiseNetworkEvent(ev, component.ActiveTarget.Value);

            int budget = GetNcpdBudget(uid, user);
            var state = new CitationDeviceBuiState(Name(component.ActiveTarget.Value), limit, budget, true);
            _uiSystem.SetUiState(uid, CitationDeviceUiKey.Key, state);
            
            _popupSystem.PopupEntity(Loc.GetString("citation-popup-sent"), uid, user);
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
                _popupSystem.PopupEntity(Loc.GetString("citation-popup-refused-cop"), uid, cop.Value);
            
            _popupSystem.PopupEntity(Loc.GetString("citation-popup-refused-target"), target, target);
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
            _popupSystem.PopupEntity(Loc.GetString("citation-popup-owner-not-found"), uid, args.Args.User);
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
            _popupSystem.PopupEntity(Loc.GetString("citation-popup-owner-not-on-server"), uid, args.Args.User);
            return;
        }

        _popupSystem.PopupEntity(Loc.GetString("citation-popup-pin-cracked"), uid, args.Args.User);
        ProcessPayment(uid, component, cardOwner.Value, args.Amount, true);
        
        args.Handled = true;
    }

    private void ProcessPayment(EntityUid terminalUid, CitationDeviceComponent component, EntityUid targetUid, int amount, bool isForced = false)
    {
        var cop = component.ActiveOfficer;
        if (cop == null) return;

        if (!_bankSystem.TryBankWithdraw(targetUid, amount))
        {
            _popupSystem.PopupEntity(Loc.GetString("citation-popup-insufficient-funds-cop"), terminalUid, cop.Value);
            if (cop != targetUid)
                _popupSystem.PopupEntity(Loc.GetString("citation-popup-insufficient-funds-target"), targetUid, targetUid);
            ResetTerminal(terminalUid, component);
            return;
        }

        int copShare = (int)(amount * component.CommissionRate);
        int deptShare = amount - copShare;

        _bankSystem.TryBankDeposit(cop.Value, copShare);

        var stationUid = GetStation(terminalUid, cop.Value);

        string jobName = Loc.GetString("citation-job-officer");
        if (_idCardSystem.TryFindIdCard(cop.Value, out var idCard) && !string.IsNullOrWhiteSpace(idCard.Comp.LocalizedJobTitle))
        {
            jobName = idCard.Comp.LocalizedJobTitle;
        }
        string fullCopName = $"{jobName} {Name(cop.Value)}";

        if (stationUid != null)
        {
            _bankSystem.TryFactionDeposit(stationUid.Value, SectorBankAccount.Ncpd, deptShare);
            _ncpdSystem.AddLog(stationUid.Value, fullCopName, Name(targetUid), amount, isForced ? Loc.GetString("citation-log-forced") : Loc.GetString("citation-log-share", ("amount", deptShare)), component.Reason);
        }

        PrintCitationPaper(terminalUid, cop.Value, fullCopName, Name(targetUid), amount, component.Reason);

        _popupSystem.PopupEntity(Loc.GetString("citation-popup-success-cop", ("amount", copShare)), terminalUid, cop.Value);
        _popupSystem.PopupEntity(Loc.GetString("citation-popup-success-target", ("amount", amount)), targetUid, targetUid);

        EnsureComp<CitationCooldownComponent>(targetUid).LastCitationTime = _timing.CurTime;

        ResetTerminal(terminalUid, component);
    }

    private void PrintCitationPaper(EntityUid terminalUid, EntityUid officerUid, string officer, string suspect, int amount, string reason)
    {
        var coords = Transform(terminalUid).Coordinates;
        var paper = Spawn("Paper", coords);
        _handsSystem.TryPickupAnyHand(officerUid, paper);
        
        if (TryComp<Content.Shared.Paper.PaperComponent>(paper, out var paperComp))
        {
            string content = $"{Loc.GetString("citation-paper-title")}\n\n" +
                             $"{Loc.GetString("citation-paper-officer", ("officer", officer))}\n" +
                             $"{Loc.GetString("citation-paper-suspect", ("suspect", suspect))}\n" +
                             $"{Loc.GetString("citation-paper-amount", ("amount", amount))}\n" +
                             $"{Loc.GetString("citation-paper-reason", ("reason", reason))}\n\n" +
                             $"{Loc.GetString("citation-paper-status")}\n" +
                             $"----------------------";
            
            _paperSystem.SetContent((paper, paperComp), content);
            
            var stamp = new Content.Shared.Paper.StampDisplayInfo
            {
                StampedName = "NCPD",
                StampedColor = Color.Blue
            };
            _paperSystem.TryStamp((paper, paperComp), stamp, "paper_stamp-corporate-nanotrasen");
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
