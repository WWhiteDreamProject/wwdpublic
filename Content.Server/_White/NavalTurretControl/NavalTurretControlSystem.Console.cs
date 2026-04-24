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
using Robust.Shared.Utility;

namespace Content.Server._White.NavalTurretControl;

public partial class NavalTurretControlSystem
{
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
        Switch(uid, comp, GetEntity(args.Turret));
    }

    private bool Switch(EntityUid consoleUid, NavalTurretConsoleComponent consoleComp, EntityUid? turretUid)
    {
        TryComp<NavalTurretComponent>(consoleComp.CurrentTurret, out var currentTurretComp);

        if (turretUid is null)
        {
            if (currentTurretComp is not null)
            {
                currentTurretComp.CurrentConsole = null;
                UpdateAllStates(currentTurretComp);
            }
            consoleComp.CurrentTurret = null;
            return true;
        }

        if (!consoleComp.LinkedTurrets.Contains(turretUid.Value))
            return false;

        if (!TryComp<NavalTurretComponent>(turretUid, out var turret))
            return false;

        if (turret.CurrentConsole is not null)
            return false;

        if(currentTurretComp is not null)
        {
            currentTurretComp.CurrentConsole = null;
            UpdateAllStates(currentTurretComp);
        }

        consoleComp.CurrentTurret = turretUid;
        turret.CurrentConsole = consoleUid;
        UpdateAllStates(turret);
        return true;
    }
}
