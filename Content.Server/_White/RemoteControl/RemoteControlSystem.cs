using Content.Server.DeviceLinking.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared._White.RemoteControl;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Power;
using Content.Shared.Verbs;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._White.RemoteControl;

public sealed class RemoteControlSystem : SharedRemoteControlSystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _link = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<RemoteControllableComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<RemoteControllableComponent, MapInitEvent>(OnCompStartup);
        SubscribeLocalEvent<RemoteControllableComponent, RemoteControlExitActionEvent>(OnEndRC);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RemoteControllingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_actionBlocker.CanConsciouslyPerformAction(uid) ||
               !_actionBlocker.CanComplexInteract(uid) ||
               !_actionBlocker.CanInteract(uid, comp.UsedInterface) || // seems to block AI
               (
               comp.UsedInterface.HasValue &&
               !_interactionSystem.InRangeAndAccessible(uid, comp.UsedInterface.Value)
               ))
                EndRemoteControl(uid);
        }
    }

    private void OnEndRC(EntityUid uid, RemoteControllableComponent comp, RemoteControlExitActionEvent args)
    {
        EndRemoteControl(comp.ControllingEntity!.Value);
    }

    private void OnCompInit(EntityUid uid, RemoteControllableComponent comp, ComponentInit args)
    {
        _link.EnsureSinkPorts(uid, "RemoteControlInputPort");
    }

    private void OnCompStartup(EntityUid uid, RemoteControllableComponent comp, MapInitEvent args)
    {
        _action.AddAction(uid, ref comp.EndRemoteControlActionEntity, comp.EndRemoteControlAction);
    }

    public void RemoteControl(EntityUid user, EntityUid target, EntityUid interfaceEntity/*, params EntityUid[] interfaceActions*/)
    {
        if (HasComp<RemoteControllingComponent>(user))
            return;

        if (!TryComp<RemoteControllableComponent>(target, out var rc))
            return;

        if (_mind.GetMind(user) is not EntityUid userMind)
            return;

        var controlling = EnsureComp<RemoteControllingComponent>(user);
        controlling.ControlledEntity = target;
        controlling.UsedInterface = interfaceEntity;

        rc.ControllingEntity = user;
        rc.ControllingMind = userMind;

        //foreach(var action in interfaceActions)
        //    _action.AddAction(target, action, interfaceEntity);

        var ev = new RemoteControlInterfaceGetActionsEvent(user, target);
        RaiseLocalEvent(interfaceEntity, ev);
        if (ev.Actions.Count == 0)
            return;
        foreach (var action in ev.Actions)
            _action.AddAction(target, action, interfaceEntity);

        var mindComp = Comp<MindComponent>(userMind);
        _mind.Visit(userMind, target, mindComp);
    }

    public void EndRemoteControl(EntityUid user, bool raiseEvent = true)
    {
        if (!TryComp<RemoteControllingComponent>(user, out var controlling))
            return;

        if (!TryComp<RemoteControllableComponent>(controlling.ControlledEntity, out var rc))
            return;

        if (_mind.GetMind(user) is not EntityUid userMind)
            return;

        rc.ControllingEntity = null;
        rc.ControllingMind = null;
        var interfaceEntity = controlling.UsedInterface;
        RemComp(user, controlling);
        _mind.UnVisit(userMind);

        if (raiseEvent && interfaceEntity is not null)
            RaiseLocalEvent(interfaceEntity.Value, new RemoteControlInterfaceEndEvent());

    }

}

public sealed class RemoteControlConsoleSystem : EntitySystem
{
    [Dependency] private readonly RemoteControlSystem _rc = default!;
    [Dependency] private readonly DeviceLinkSystem _link = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RemoteControlConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<RemoteControlConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<RemoteControlConsoleComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<RemoteControlConsoleComponent, RemoteControlConsoleSwitchNextActionEvent>(OnNextAction);
        SubscribeLocalEvent<RemoteControlConsoleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RemoteControlConsoleComponent, RemoteControlInterfaceEndEvent>(OnRemoteControlEnd);
        SubscribeLocalEvent<RemoteControlConsoleComponent, RemoteControlInterfaceGetActionsEvent>(OnRCGetActionsEvent);
        SubscribeLocalEvent<RemoteControlConsoleComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnRemoteControlEnd(EntityUid uid, RemoteControlConsoleComponent comp, RemoteControlInterfaceEndEvent args)
    {
        comp.CurrentUser = null;
    }

    private void OnPowerChanged(EntityUid uid, RemoteControlConsoleComponent comp, PowerChangedEvent args)
    {
        if(!args.Powered && comp.CurrentUser is not null)
            _rc.EndRemoteControl(comp.CurrentUser.Value);
    }

    private void OnInit(EntityUid uid, RemoteControlConsoleComponent comp, ComponentInit args)
    {
        _link.EnsureSourcePorts(uid, comp.ConnectionPortId);
    }

    private void OnNewLink(EntityUid uid, RemoteControlConsoleComponent comp, NewLinkEvent args)
    {
        if (args.Source == uid && args.SourcePort == comp.ConnectionPortId && HasComp<RemoteControllableComponent>(args.Sink))
            comp.LinkedEntities.Add(args.Sink);
    }

    private void OnPortDisconnected(EntityUid uid, RemoteControlConsoleComponent comp, PortDisconnectedEvent args)
    {
        if (args.Port == comp.ConnectionPortId)
            comp.LinkedEntities.Remove(args.RemovedPortUid);
    }

    private void OnNextAction(EntityUid uid, RemoteControlConsoleComponent comp, RemoteControlConsoleSwitchNextActionEvent args)
    {
        if (!TryComp<RemoteControllableComponent>(args.Performer, out var rc))
            return;

        if (comp.CurrentUser != rc.ControllingEntity || comp.CurrentUser is null)
            return; // probably worth a warning in logs

        if (GetFirstValid(comp.LinkedEntities, comp.LastIndex, args.Performer) is not EntityUid target || rc.ControllingEntity == target)
            return; // no other targets available

        comp.LastIndex = comp.LinkedEntities.IndexOf(target);

        _rc.EndRemoteControl(comp.CurrentUser.Value, false);
        //_rc.RemoteControl(comp.CurrentUser.Value, target, uid, comp.SwitchToNextActionEntity!.Value);
        _rc.RemoteControl(comp.CurrentUser.Value, target, uid);
    }

    private void OnActivateInWorld(EntityUid uid, RemoteControlConsoleComponent comp, ActivateInWorldEvent args)
    {
        if (!this.IsPowered(uid, EntityManager))
            return;

        if (HasComp<RemoteControllingComponent>(args.User))
            return;

        if (comp.CurrentUser is not null)
            return; // in use

        if (comp.LinkedEntities.Count == 0)
            return; // none connected

        if (GetFirstValid(comp.LinkedEntities, comp.LastIndex) is not EntityUid target)
            return; // none available

        comp.CurrentUser = args.User;
        comp.LastIndex = comp.LinkedEntities.IndexOf(target);

        //_rc.RemoteControl(args.User, target, uid, comp.SwitchToNextActionEntity!.Value);
        _rc.RemoteControl(args.User, target, uid);
    }

    private void OnRCGetActionsEvent(EntityUid uid, RemoteControlConsoleComponent comp, RemoteControlInterfaceGetActionsEvent args)
    {
        if (comp.LinkedEntities.Count > 1)
        {
            _actionContainer.EnsureAction(uid, ref comp.SwitchToNextActionEntity, comp.SwitchToNextAction);
            args.Actions.Add(comp.S);
        }
    }


    private EntityUid? GetFirstValid(List<EntityUid> list, int startIndex, EntityUid? exclude = null)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int index = (i + startIndex ) % list.Count;
            var ent = list[index];
            if ((!exclude.HasValue || ent != exclude) && IsValidTarget(ent))
                return ent;
        }
        return null;
    }

    private bool IsValidTarget(EntityUid target)
    {
        if (!HasComp<RemoteControllableComponent>(target))
            return false;

        if (TryComp<MindContainerComponent>(target, out var mindCont) &&
            mindCont.HasMind)
            return false;

        return true;
    }

}

public sealed partial class RemoteControlInterfaceGetActionsEvent(EntityUid user, EntityUid target) : EntityEventArgs
{
    public readonly EntityUid Target = target;
    public readonly EntityUid User = user;
    public List<EntityUid> Actions = new();
}

public sealed partial class RemoteControlInterfaceEndEvent : EntityEventArgs;
