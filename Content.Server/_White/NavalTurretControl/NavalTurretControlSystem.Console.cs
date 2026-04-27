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
        TrySwitch(uid, comp, GetEntity(args.Turret), _actor.GetSession(args.Actor));
    }

    private bool TrySwitch(EntityUid consoleUid, NavalTurretConsoleComponent consoleComp, EntityUid? newTurretUid, ICommonSession? player)
    {
        NavalTurretComponent? currentTurretComp;
        var currentTurretUid = consoleComp.CurrentTurret;
        if (newTurretUid is null)
        {
            consoleComp.CurrentTurret = null;
            if (TryComp<NavalTurretComponent>(currentTurretUid, out currentTurretComp))
            {
                RemoveFromPvsOverride(consoleUid, consoleComp.CurrentTurret);
                currentTurretComp.CurrentConsole = null;
                Dirty(currentTurretUid.Value, currentTurretComp);
                UpdateAllStates(currentTurretComp);
            }
            return true;
        }

        if (!_link.IsConnectedToSource(consoleUid, SourcePortId, newTurretUid.Value))
            return false;

        if (!TryComp<NavalTurretComponent>(newTurretUid, out var newTurretComp))
            return false;

        if (newTurretComp.CurrentConsole is not null)
            return false;

        if (TryComp<NavalTurretComponent>(consoleComp.CurrentTurret, out currentTurretComp))
        {
            currentTurretComp.CurrentConsole = null;
            Dirty(consoleComp.CurrentTurret.Value, currentTurretComp);
            UpdateStateForAllConnected(currentTurretComp);
        }

        RemoveFromPvsOverride(consoleUid, consoleComp.CurrentTurret);
        AddToPvsOverride(consoleUid, newTurretUid);
        consoleComp.CurrentTurret = newTurretUid;
        newTurretComp.CurrentConsole = consoleUid;
        Dirty(newTurretUid.Value, newTurretComp);
        UpdateStateForAllConnected(newTurretComp);
        return true;
    }

    protected override void OnUiOpen(EntityUid uid, NavalTurretConsoleComponent comp, BoundUIOpenedEvent args)
    {
        base.OnUiOpen(uid, comp, args);
        if (comp.CurrentTurret is not EntityUid turret)
            return;

        var session = _actor.GetSession(args.Actor);
        DebugTools.Assert(session is not null);
        _pvs.AddSessionOverride(turret, session);
    }

    protected override void OnUiClosed(EntityUid uid, NavalTurretConsoleComponent comp, BoundUIClosedEvent args)
    {
        base.OnUiClosed(uid, comp, args);
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
