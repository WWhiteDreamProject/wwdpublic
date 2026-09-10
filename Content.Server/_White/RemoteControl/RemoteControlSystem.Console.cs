using Content.Server.Power.EntitySystems;
using Content.Shared._White.RemoteControl;
using Content.Shared._White.RemoteControl.BUIStates;
using Content.Shared._White.MindProjection;
using Content.Shared._White.MindProjection.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Power;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._White.RemoteControl;

public partial class RemoteControlSystem
{
    [Dependency] private readonly SharedPvsOverrideSystem _pvs = default!;
    [Dependency] private readonly ActorSystem _actor = default!;

    private void InitializeConsole()
    {
        SubscribeLocalEvent<RemoteControlConsoleComponent, ComponentInit>(OnConsoleInit);

        SubscribeLocalEvent<RemoteControlConsoleComponent, LinkAttemptEvent>(OnNewLinkToConsoleAttempt);
        SubscribeLocalEvent<RemoteControlConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<RemoteControlConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);

        SubscribeLocalEvent<RemoteControlConsoleComponent, RemoteControlConsoleTurretSelectedBuiMessage>(OnTurretSelection);
    }

    private void OnConsoleInit(EntityUid uid, RemoteControlConsoleComponent comp, ComponentInit args)
    {
        _link.EnsureSourcePorts(uid, SourcePortId);
    }

    //private void OnUseInHand(EntityUid uid, RemoteControlConsoleComponent component, UseInHandEvent args) =>
    //    TryActivate(uid, component, args.User);
    //private void OnActivateInWorld(EntityUid uid, RemoteControlConsoleComponent component, ActivateInWorldEvent args) =>
    //    TryActivate(uid, component, args.User);

    private void OnNewLinkToConsoleAttempt(EntityUid uid, RemoteControlConsoleComponent component, LinkAttemptEvent args)
    {
        if (args.Source != uid || args.SourcePort != SourcePortId)
            return;

        if (!TryComp<RemoteControllableComponent>(args.Sink, out var turretComp) ||
            args.Source != uid ||
            args.SourcePort != SourcePortId ||
            args.SinkPort != SinkPortId)
            args.Cancel();
    }

    private void OnNewLink(EntityUid uid, RemoteControlConsoleComponent component, NewLinkEvent args)
    {
        if (args.Source != uid || args.SourcePort != SourcePortId)
            return;

        var turretUid = args.Sink;
        if (!TryComp<RemoteControllableComponent>(turretUid, out var turretComp))
        {
            Log.Error($"{ToPrettyString(uid)} was connected to {ToPrettyString(args.Sink)}, which lacks {nameof(RemoteControllableComponent)}. This should not be possible.");
            DebugTools.Assert(false);
            return;
        }

        UpdateState(uid, component);
    }

    private void OnPortDisconnected(EntityUid uid, RemoteControlConsoleComponent component, PortDisconnectedEvent args)
    {
        if (args.Port != SourcePortId)
            return;

        UpdateState(uid, component);
    }

    private void OnTurretSelection(EntityUid uid, RemoteControlConsoleComponent comp, RemoteControlConsoleTurretSelectedBuiMessage args)
    {
        TrySwitch(uid, comp, GetEntity(args.Turret));
    }

    private bool TrySwitch(EntityUid consoleUid, RemoteControlConsoleComponent consoleComp, EntityUid? newTurretUid)
    {
        RemoteControllableComponent? currentTurretComp;
        var currentTurretUid = consoleComp.CurrentTurret;
        if (newTurretUid is null)
        {
            consoleComp.CurrentTurret = null;
            if (TryComp<RemoteControllableComponent>(currentTurretUid, out currentTurretComp))
            {
                RemoveFromPvsOverride(consoleUid, currentTurretUid.Value);
                currentTurretComp.CurrentConsole = null;
                Dirty(currentTurretUid.Value, currentTurretComp);
                UpdateStateForAllConnected(currentTurretUid.Value);
            }
            return true;
        }

        if (!_link.IsConnectedToSource(consoleUid, SourcePortId, newTurretUid.Value))
            return false;

        if (!TryComp<RemoteControllableComponent>(newTurretUid, out var newTurretComp))
            return false;

        if (newTurretComp.CurrentConsole is not null)
            return false;

        if (TryComp<RemoteControllableComponent>(currentTurretUid, out currentTurretComp))
        {
            currentTurretComp.CurrentConsole = null;
            Dirty(currentTurretUid.Value, currentTurretComp);
            UpdateStateForAllConnected(currentTurretUid.Value);
            RemoveFromPvsOverride(consoleUid, currentTurretUid.Value);
        }

        AddToPvsOverride(consoleUid, newTurretUid.Value);
        consoleComp.CurrentTurret = newTurretUid;
        newTurretComp.CurrentConsole = consoleUid;
        Dirty(newTurretUid.Value, newTurretComp);
        UpdateStateForAllConnected(newTurretUid.Value);
        return true;
    }

    protected override void OnUiOpen(EntityUid uid, RemoteControlConsoleComponent comp, BoundUIOpenedEvent args)
    {
        base.OnUiOpen(uid, comp, args);
        if (comp.CurrentTurret is not EntityUid turret)
            return;

        var session = _actor.GetSession(args.Actor);
        DebugTools.Assert(session is not null);
        _pvs.AddSessionOverride(turret, session);
    }

    protected override void OnUiClosed(EntityUid uid, RemoteControlConsoleComponent comp, BoundUIClosedEvent args)
    {
        base.OnUiClosed(uid, comp, args);
        if (comp.CurrentTurret is not EntityUid turret)
            return;

        var session = _actor.GetSession(args.Actor);
        DebugTools.Assert(session is not null);
        _pvs.RemoveSessionOverride(turret, session);
        TrySwitch(uid, comp, null);
    }

    private void RemoveFromPvsOverride(Entity<UserInterfaceComponent?> consoleEntity, EntityUid obj)
    {
        foreach (var actor in _uiSystem.GetActors(consoleEntity, RemoteControlConsoleUiKey.Key))
        {
            var session = _actor.GetSession(actor);
            DebugTools.Assert(session is not null);
            _pvs.RemoveSessionOverride(obj, session);
        }
    }

    private void AddToPvsOverride(Entity<UserInterfaceComponent?> consoleEntity, EntityUid obj)
    {
        foreach (var actor in _uiSystem.GetActors(consoleEntity, RemoteControlConsoleUiKey.Key))
        {
            var session = _actor.GetSession(actor);
            DebugTools.Assert(session is not null);
            _pvs.AddSessionOverride(obj, session);
        }
    }
}
