using Content.Server.Power.EntitySystems;
using Content.Shared._White.RemoteControl;
using Content.Shared._White.RemoteControl.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Power;

namespace Content.Server._White.RemoteControl.Systems;

public partial class RemoteControlSystem
{
    private void InitializeConsole()
    {
        SubscribeLocalEvent<RemoteControlConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<RemoteControlConsoleComponent, MapInitEvent>(OnConsoleMapInit);
        SubscribeLocalEvent<RemoteControlConsoleComponent, ComponentShutdown>(OnConsoleShutdown);

        SubscribeLocalEvent<RemoteControlConsoleComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<RemoteControlConsoleComponent, UseInHandEvent>(OnUseInHand);

        SubscribeLocalEvent<RemoteControlConsoleComponent, LinkAttemptEvent>(OnNewLinkAttempt);
        SubscribeLocalEvent<RemoteControlConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<RemoteControlConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);

        SubscribeLocalEvent<RemoteControlConsoleComponent, RemoteControlConsoleSwitchNextActionEvent>(OnNextAction);

        SubscribeLocalEvent<RemoteControlConsoleComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnConsoleInit(EntityUid uid, RemoteControlConsoleComponent comp, ComponentInit args)
    {
        _link.EnsureSourcePorts(uid, SourcePortId);
    }

    private void OnConsoleMapInit(EntityUid uid, RemoteControlConsoleComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.SwitchToNextActionUid, component.SwitchToNextAction);
    }

    private void OnConsoleShutdown(EntityUid uid, RemoteControlConsoleComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.SwitchToNextActionUid);

        if (component.User.HasValue)
            EndRemoteControl(component.User.Value, (uid, component));
    }

    private void OnUseInHand(EntityUid uid, RemoteControlConsoleComponent component, UseInHandEvent args) =>
        TryActivate(uid, component, args.User);

    private void OnActivateInWorld(EntityUid uid, RemoteControlConsoleComponent component, ActivateInWorldEvent args) =>
        TryActivate(uid, component, args.User);

    private void OnNewLinkAttempt(EntityUid uid, RemoteControlConsoleComponent component, LinkAttemptEvent args)
    {
        if (args.Source != uid || args.SourcePort != SourcePortId)
            return;

        if (!HasComp<RemoteControllableComponent>(args.Sink) ||
            !_whitelist.CheckBoth(args.Sink, component.Blacklist, component.Whitelist) ||
            args.Source != uid ||
            args.SourcePort != SourcePortId ||
            args.SinkPort != SinkPortId)
            args.Cancel();
    }
    private void OnNewLink(EntityUid uid, RemoteControlConsoleComponent component, NewLinkEvent args)
    {
        if (args.Source == uid && args.SourcePort == SourcePortId)
            component.LinkedEntities.Add(args.Sink);
    }

    private void OnPortDisconnected(EntityUid uid, RemoteControlConsoleComponent component, PortDisconnectedEvent args)
    {
        if (args.Port != SourcePortId)
            return;

        component.LinkedEntities.Remove(args.RemovedPortUid);

        // the device link got severed while the turret was in use; either relink to another turret or kick the user out of the console.
        if (component.User is { } user
            && TryComp<RemoteControllingComponent>(user, out var controlling)
            && controlling.Target == args.RemovedPortUid)
            EndRemoteControl(user, (uid, component), true);
    }

    private void OnNextAction(EntityUid uid, RemoteControlConsoleComponent component, RemoteControlConsoleSwitchNextActionEvent args) =>
        TrySwitchToNextAvailable(uid, component);

    private void OnPowerChanged(EntityUid uid, RemoteControlConsoleComponent component, PowerChangedEvent args)
    {
        // this will raise RemoteControlInterfaceEndEvent directed on the console, which is handled above
        // hence we don't need to clear comp.CurrentUser here
        if (!args.Powered && component.User is not null)
            EndRemoteControl(component.User.Value, (uid, component));
    }

    private void TryActivate(EntityUid uid, RemoteControlConsoleComponent component, EntityUid user)
    {
        if (!this.IsPowered(uid, EntityManager)
            || HasComp<RemoteControllingComponent>(user)
            || component.User is not null
            || component.LinkedEntities.Count == 0
            || GetFirstValid(component) is not { } target)
            return;

        RemoteControl(user, target, (uid, component), true);
    }

    private bool TrySwitchToNextAvailable(EntityUid console, RemoteControlConsoleComponent component)
    {
        if (component.User is not { } user || GetFirstValid(component, component.Target) is not { } target)
            return false;

        component.LastIndex = component.LinkedEntities.IndexOf(target);

        if (component.Target.HasValue)
            EndRemoteControl(user, component.Target.Value, (console, component));

        return RemoteControl(user, target, (console, component), true);
    }

    private EntityUid? GetFirstValid(RemoteControlConsoleComponent component, EntityUid? exclude = null) =>
        GetFirstValid(component.LinkedEntities, component.LastIndex, exclude);

    private EntityUid? GetFirstValid(List<EntityUid> list, int startIndex, EntityUid? exclude = null)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var index = (i + startIndex) % list.Count;
            var ent = list[index];

            if ((!exclude.HasValue || ent != exclude)
                && HasComp<RemoteControllableComponent>(ent)
                || TryComp<MindContainerComponent>(ent, out var mindContainer)
                && mindContainer.HasMind)
                return ent;
        }

        return null;
    }
}
