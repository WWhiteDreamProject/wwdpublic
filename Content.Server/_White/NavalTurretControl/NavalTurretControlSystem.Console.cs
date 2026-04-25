using Content.Server.Power.EntitySystems;
using Content.Shared._White.NavalTurretControl;
using Content.Shared._White.NavalTurretControl.BUIStates;
using Content.Shared._White.RemoteControl;
using Content.Shared._White.RemoteControl.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Power;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._White.NavalTurretControl;

public partial class NavalTurretControlSystem
{
    [Dependency] private readonly SharedPvsOverrideSystem _pvs = default!;
    [Dependency] private readonly ActorSystem _actor = default!;

    private void InitializeConsole()
    {
        SubscribeLocalEvent<NavalTurretConsoleComponent, ComponentInit>(OnConsoleInit);

        SubscribeLocalEvent<NavalTurretConsoleComponent, LinkAttemptEvent>(OnNewLinkToConsoleAttempt);
        SubscribeLocalEvent<NavalTurretConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<NavalTurretConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);

        SubscribeLocalEvent<NavalTurretConsoleComponent, NavalTurretConsoleTurretSelectedBuiMessage>(OnTurretSelection);
        SubscribeLocalEvent<NavalTurretConsoleComponent, BoundUIOpenedEvent>(OnUiOpen);
        SubscribeLocalEvent<NavalTurretConsoleComponent, BoundUIClosedEvent>(OnUiClosed);

    }

    private void OnConsoleInit(EntityUid uid, NavalTurretConsoleComponent comp, ComponentInit args)
    {
        _link.EnsureSourcePorts(uid, SourcePortId);
    }

    //private void OnUseInHand(EntityUid uid, NavalTurretConsoleComponent component, UseInHandEvent args) =>
    //    TryActivate(uid, component, args.User);
    //private void OnActivateInWorld(EntityUid uid, NavalTurretConsoleComponent component, ActivateInWorldEvent args) =>
    //    TryActivate(uid, component, args.User);

    private void OnNewLinkToConsoleAttempt(EntityUid uid, NavalTurretConsoleComponent component, LinkAttemptEvent args)
    {
        if (args.Source != uid || args.SourcePort != SourcePortId)
            return;

        if (!TryComp<NavalTurretComponent>(args.Sink, out var turretComp) ||
            args.Source != uid ||
            args.SourcePort != SourcePortId ||
            args.SinkPort != SinkPortId)
            args.Cancel();
    }

    private void OnNewLink(EntityUid uid, NavalTurretConsoleComponent component, NewLinkEvent args)
    {
        if (args.Source != uid || args.SourcePort != SourcePortId)
            return;
        var turretUid = args.Sink;

        if (!TryComp<NavalTurretComponent>(turretUid, out var turretComp))
        {
            Log.Error($"{ToPrettyString(uid)} was connected to {ToPrettyString(args.Sink)}, which lacks {nameof(NavalTurretComponent)}. This should not be possible.");
            DebugTools.Assert(false);
            return;
        }

        component.LinkedTurrets.Add(turretUid);
        turretComp.LinkedConsoles.Add(uid);
        UpdateState(uid, component);
    }

    private void OnPortDisconnected(EntityUid uid, NavalTurretConsoleComponent component, PortDisconnectedEvent args)
    {
        if (args.Port != SourcePortId)
            return;

        var turretUid = args.RemovedPortUid;

        var succ = component.LinkedTurrets.Remove(turretUid);
        DebugTools.Assert(succ, $"Attempted to disconnect a turret that was not tracked in console's LinkedTurrets in the first place.");

        if(TryComp<NavalTurretComponent>(turretUid, out var turret)) // no assert because the entity might have been disconnected due to deletion
        {
            var succ2 = turret.LinkedConsoles.Remove(uid);
            DebugTools.Assert(succ2, $"Attempted to disconnect a console that was not tracked in turret's LinkedConsoles in the first place.");
        }
        UpdateState(uid, component);
    }

    private void OnTurretSelection(EntityUid uid, NavalTurretConsoleComponent comp, NavalTurretConsoleTurretSelectedBuiMessage args)
    {
        Switch(uid, comp, GetEntity(args.Turret), _actor.GetSession(args.Actor));
    }

    private bool Switch(EntityUid consoleUid, NavalTurretConsoleComponent consoleComp, EntityUid? newTurretUid, ICommonSession? player)
    {
        NavalTurretComponent? currentTurretComp;
        if (newTurretUid is null)
        {
            if (TryComp<NavalTurretComponent>(consoleComp.CurrentTurret, out currentTurretComp))
            {
                RemoveFromPvsOverride(consoleUid, consoleComp.CurrentTurret);
                currentTurretComp.CurrentConsole = null;
                UpdateAllStates(currentTurretComp);
            }
            consoleComp.CurrentTurret = null;
            return true;
        }

        if (!consoleComp.LinkedTurrets.Contains(newTurretUid.Value))
            return false;

        if (!TryComp<NavalTurretComponent>(newTurretUid, out var newTurretComp))
            return false;

        if (newTurretComp.CurrentConsole is not null)
            return false;

        if (TryComp<NavalTurretComponent>(consoleComp.CurrentTurret, out currentTurretComp))
        {
            currentTurretComp.CurrentConsole = null;
            UpdateAllStates(currentTurretComp);
        }

        RemoveFromPvsOverride(consoleUid, consoleComp.CurrentTurret);
        AddToPvsOverride(consoleUid, newTurretUid);
        consoleComp.CurrentTurret = newTurretUid;
        newTurretComp.CurrentConsole = consoleUid;
        UpdateAllStates(newTurretComp);
        return true;
    }

    private void OnUiOpen(EntityUid uid, NavalTurretConsoleComponent comp, BoundUIOpenedEvent args)
    {
        if (comp.CurrentTurret is not EntityUid turret)
            return;

        var session = _actor.GetSession(args.Actor);
        DebugTools.Assert(session is not null);
        _pvs.AddSessionOverride(turret, session);
    }

    private void OnUiClosed(EntityUid uid, NavalTurretConsoleComponent comp, BoundUIClosedEvent args)
    {
        if (comp.CurrentTurret is not EntityUid turret)
            return;

        var session = _actor.GetSession(args.Actor);
        DebugTools.Assert(session is not null);
        _pvs.RemoveSessionOverride(turret, session);
    }


    private void RemoveFromPvsOverride(Entity<UserInterfaceComponent?> consoleEntity, EntityUid? obj)
    {
        if (obj is null)
            return;

        foreach (var actor in _uiSystem.GetActors(consoleEntity, NavalTurretConsoleUiKey.Key))
        {
            var session = _actor.GetSession(actor);
            DebugTools.Assert(session is not null);
            _pvs.RemoveSessionOverride(obj.Value, session);
        }
    }

    private void AddToPvsOverride(Entity<UserInterfaceComponent?> consoleEntity, EntityUid? obj)
    {
        if (obj is null)
            return;

        foreach (var actor in _uiSystem.GetActors(consoleEntity, NavalTurretConsoleUiKey.Key))
        {
            var session = _actor.GetSession(actor);
            DebugTools.Assert(session is not null);
            _pvs.AddSessionOverride(obj.Value, session);
        }
    }
}
