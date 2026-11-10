using Content.Server.Power.EntitySystems;
using Content.Shared._White.MindProjection;
using Content.Shared._White.MindProjection.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Power;

namespace Content.Server._White.MindProjection.Systems;

public partial class MindProjectionSystem
{
    // TODO: consider removing this and replace with something more fitting
    //       than a console you simply stand next to and click on
    private void InitializeConsole()
    {
        SubscribeLocalEvent<MindProjectionConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<MindProjectionConsoleComponent, MapInitEvent>(OnConsoleMapInit);
        SubscribeLocalEvent<MindProjectionConsoleComponent, ComponentShutdown>(OnConsoleShutdown);

        SubscribeLocalEvent<MindProjectionConsoleComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<MindProjectionConsoleComponent, UseInHandEvent>(OnUseInHand);

        SubscribeLocalEvent<MindProjectionConsoleComponent, LinkAttemptEvent>(OnNewLinkAttempt);
        SubscribeLocalEvent<MindProjectionConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<MindProjectionConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);

        SubscribeLocalEvent<MindProjectionConsoleComponent, RemoteControlConsoleSwitchNextActionEvent>(OnNextAction);

        SubscribeLocalEvent<MindProjectionConsoleComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnConsoleInit(EntityUid uid, MindProjectionConsoleComponent comp, ComponentInit args)
    {
        _link.EnsureSourcePorts(uid, SourcePortId);
    }

    private void OnConsoleMapInit(EntityUid uid, MindProjectionConsoleComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.SwitchToNextActionUid, component.SwitchToNextAction);
    }

    private void OnConsoleShutdown(EntityUid uid, MindProjectionConsoleComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.SwitchToNextActionUid);

        if (component.User.HasValue)
            EndRemoteControl(component.User.Value, (uid, component));
    }

    private void OnUseInHand(EntityUid uid, MindProjectionConsoleComponent component, UseInHandEvent args) =>
        TryActivate(uid, component, args.User);

    private void OnActivateInWorld(EntityUid uid, MindProjectionConsoleComponent component, ActivateInWorldEvent args) =>
        TryActivate(uid, component, args.User);

    private void OnNewLinkAttempt(EntityUid uid, MindProjectionConsoleComponent component, LinkAttemptEvent args)
    {
        if (args.Source != uid || args.SourcePort != SourcePortId)
            return;

        if (!HasComp<MindProjectionTargetComponent>(args.Sink) ||
            !_whitelist.CheckBoth(args.Sink, component.Blacklist, component.Whitelist) ||
            args.Source != uid ||
            args.SourcePort != SourcePortId ||
            args.SinkPort != SinkPortId)
            args.Cancel();
    }
    private void OnNewLink(EntityUid uid, MindProjectionConsoleComponent component, NewLinkEvent args)
    {
        if (args.Source == uid && args.SourcePort == SourcePortId)
            component.LinkedEntities.Add(args.Sink);
    }

    private void OnPortDisconnected(EntityUid uid, MindProjectionConsoleComponent component, PortDisconnectedEvent args)
    {
        if (args.Port != SourcePortId)
            return;

        component.LinkedEntities.Remove(args.RemovedPortUid);

        // the device link got severed while the turret was in use; either relink to another turret or kick the user out of the console.
        if (component.User is { } user
            && TryComp<MingProjectingComponent>(user, out var controlling)
            && controlling.Target == args.RemovedPortUid)
            EndRemoteControl(user, (uid, component), true);
    }

    private void OnNextAction(EntityUid uid, MindProjectionConsoleComponent component, RemoteControlConsoleSwitchNextActionEvent args) =>
        TrySwitchToNextAvailable(uid, component);

    private void OnPowerChanged(EntityUid uid, MindProjectionConsoleComponent component, PowerChangedEvent args)
    {
        // this will raise RemoteControlInterfaceEndEvent directed on the console, which is handled above
        // hence we don't need to clear comp.CurrentUser here
        if (!args.Powered && component.User is not null)
            EndRemoteControl(component.User.Value, (uid, component));
    }

    private void TryActivate(EntityUid uid, MindProjectionConsoleComponent component, EntityUid user)
    {
        if (!this.IsPowered(uid, EntityManager)
            || HasComp<MingProjectingComponent>(user)
            || component.User is not null
            || component.LinkedEntities.Count == 0
            || GetFirstValid(component) is not { } target)
            return;

        RemoteControl(user, target, (uid, component), true);
    }

    private bool TrySwitchToNextAvailable(EntityUid console, MindProjectionConsoleComponent component)
    {
        if (component.User is not { } user || GetFirstValid(component, component.Target) is not { } target)
            return false;

        component.LastIndex = component.LinkedEntities.IndexOf(target);

        if (component.Target.HasValue)
            EndRemoteControl(user, component.Target.Value, (console, component));

        return RemoteControl(user, target, (console, component), true);
    }

    private EntityUid? GetFirstValid(MindProjectionConsoleComponent component, EntityUid? exclude = null) =>
        GetFirstValid(component.LinkedEntities, component.LastIndex, exclude);

    private EntityUid? GetFirstValid(List<EntityUid> list, int startIndex, EntityUid? exclude = null)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var index = (i + startIndex) % list.Count;
            var ent = list[index];

            if ((!exclude.HasValue || ent != exclude)
                && HasComp<MindProjectionTargetComponent>(ent)
                || TryComp<MindContainerComponent>(ent, out var mindContainer)
                && mindContainer.HasMind)
                return ent;
        }

        return null;
    }
}
