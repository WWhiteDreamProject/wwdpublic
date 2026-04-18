using Content.Server.Power.EntitySystems;
using Content.Shared._White.NavalTurretControl;
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
        SubscribeLocalEvent<NavalTurretComponent, LinkAttemptEvent>(OnNewLinkToTurretAttempt);
        SubscribeLocalEvent<NavalTurretConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<NavalTurretConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);

        SubscribeLocalEvent<NavalTurretConsoleComponent, PowerChangedEvent>(OnPowerChanged);
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
            component.LinkedTurret != null ||
            turretComp.LinkedConsole != null ||
            args.Source != uid ||
            args.SourcePort != SourcePortId ||
            args.SinkPort != SinkPortId)
            args.Cancel();
    }

    private void OnNewLinkToTurretAttempt(EntityUid uid, NavalTurretComponent component, LinkAttemptEvent args)
    {
        if (args.Source != uid || args.SourcePort != SourcePortId)
            return;

        if (!TryComp<NavalTurretConsoleComponent>(args.Sink, out var consoleComp) ||
            component.LinkedConsole != null ||
            consoleComp.LinkedTurret != null ||
            args.Source != uid ||
            args.SourcePort != SourcePortId ||
            args.SinkPort != SinkPortId)
            args.Cancel();
    }
    private void OnNewLink(EntityUid uid, NavalTurretConsoleComponent component, NewLinkEvent args)
    {
        if (args.Source != uid || args.SourcePort != SourcePortId)
            return;

        component.LinkedTurret = args.Sink;
        var succ = TryComp<NavalTurretComponent>(args.Sink, out var turretComp);
        DebugTools.Assert(succ);
        turretComp!.LinkedConsole = uid;
        Dirty(uid, component);
        UpdateState(uid, component);
    }

    private void OnPortDisconnected(EntityUid uid, NavalTurretConsoleComponent component, PortDisconnectedEvent args)
    {
        if (args.Port != SourcePortId)
            return;

        component.LinkedTurret = null;
        var succ = TryComp<NavalTurretComponent>(component.LinkedTurret, out var turretComp);
        DebugTools.Assert(succ);
        turretComp!.LinkedConsole = null;
        Dirty(uid, component);
        UpdateState(uid, component);
    }

    private void OnPowerChanged(EntityUid uid, NavalTurretConsoleComponent component, PowerChangedEvent args)
    {
        UpdateState(uid, component);
    }

}
