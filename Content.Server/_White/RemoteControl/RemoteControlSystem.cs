using Content.Server.Chat.Systems;
using Content.Server.DeviceLinking.Systems;
using Content.Server.GameTicking;
using Content.Server.Power.EntitySystems;
using Content.Shared._White.RemoteControl;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power;
using Content.Shared.Whitelist;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using static Content.Server.Chat.Systems.ChatSystem;

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
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<RemoteControllableComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<RemoteControllableComponent, GhostAttemptHandleEvent>(OnGhostAttempt);
        SubscribeLocalEvent<RemoteControllableComponent, MapInitEvent>(OnCompMapInit);
        SubscribeLocalEvent<RemoteControllableComponent, RemoteControlExitActionEvent>(OnEndRC);
        SubscribeLocalEvent<RemoteControllableComponent, SpeechSourceOverrideEvent>(OnSpeechSourceOverride);
        SubscribeLocalEvent<RemoteControllableComponent, MobStateChangedEvent>(OnControllableMobStateChanged);
        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandICChatRecipients);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RemoteControllingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if(ShouldEndRC(uid, comp))
                EndRemoteControl(uid);
        }

        bool ShouldEndRC(EntityUid uid, RemoteControllingComponent comp)
        {
            if (!_actionBlocker.CanInteract(uid, comp.UsedInterface))
                return true;

            if (comp.UsedInterface.HasValue && !_interactionSystem.InRangeAndAccessible(uid, comp.UsedInterface.Value))
                return true;

            if (TryComp<MobStateComponent>(comp.ControlledEntity, out var mobState) && !_mobState.IsAlive(comp.ControlledEntity, mobState))
                return true;

            return false;
        }
    }

    private void OnControllableMobStateChanged(EntityUid uid, RemoteControllableComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive && comp.ControllingEntity is EntityUid controller)
            EndRemoteControl(controller);
    }

    private void OnGhostAttempt(EntityUid uid, RemoteControllableComponent comp, GhostAttemptHandleEvent args)
    {
        if (comp.ControllingMind is not null)
        {
            args.Handled = true;
            args.Result = false;
        }
    }

    private void OnSpeechSourceOverride(EntityUid uid, RemoteControllableComponent comp, SpeechSourceOverrideEvent args)
    {
        if (comp.ControllingEntity is EntityUid controller)
            args.Override = controller;
    }

    private void OnExpandICChatRecipients(ExpandICChatRecipientsEvent args)
    {
        var query = EntityQueryEnumerator<RemoteControllingComponent, TransformComponent>();
        var sourceXform = Transform(args.Source);
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            var range = (xform.MapID != sourceXform.MapID)
            ? -1
            : (_transform.GetWorldPosition(sourceXform) - _transform.GetWorldPosition(xform)).Length();

            if (range < 0 || range > args.VoiceRange)
                continue;

            if (TryComp<ActorComponent>(comp.ControlledEntity, out var actor))
            {
                args.Recipients.TryAdd(actor.PlayerSession, new ICChatRecipientData(range, false, false));
            }
        }
    }

    private void OnEndRC(EntityUid uid, RemoteControllableComponent comp, RemoteControlExitActionEvent args)
    {
        if(comp.ControllingEntity is not null)
            EndRemoteControl(comp.ControllingEntity.Value);
    }

    private void OnCompInit(EntityUid uid, RemoteControllableComponent comp, ComponentInit args)
    {
        _link.EnsureSinkPorts(uid, "RemoteControlInputPort");
    }

    private void OnCompMapInit(EntityUid uid, RemoteControllableComponent comp, MapInitEvent args)
    {
        _action.AddAction(uid, ref comp.EndRemoteControlActionEntity, comp.EndRemoteControlAction);
    }

    public bool RemoteControl(EntityUid user, EntityUid target, EntityUid interfaceEntity/*, params EntityUid[] interfaceActions*/)
    {
        if (HasComp<RemoteControllingComponent>(user))
            return false;

        if (!TryComp<RemoteControllableComponent>(target, out var rc))
            return false;

        if (_mind.GetMind(user) is not EntityUid userMind)
            return false;

        var controlling = EnsureComp<RemoteControllingComponent>(user);
        controlling.ControlledEntity = target;
        controlling.UsedInterface = interfaceEntity;

        rc.ControllingEntity = user;
        rc.ControllingMind = userMind;

        var ev = new RemoteControlInterfaceGetActionsEvent(user, target);
        RaiseLocalEvent(interfaceEntity, ev);
        foreach (var action in ev.Actions)
            _action.AddAction(target, action, interfaceEntity);

        var mindComp = Comp<MindComponent>(userMind);
        _mind.Visit(userMind, target, mindComp);
        return true;
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
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RemoteControlConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<RemoteControlConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<RemoteControlConsoleComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<RemoteControlConsoleComponent, UseInHandEvent>(OnUseInHand);
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
        // this will raise RemoteControlInterfaceEndEvent directed on the console, which is handled above
        // hence we don't need to clear comp.CurrentUser here
        if (!args.Powered && comp.CurrentUser is not null)
            _rc.EndRemoteControl(comp.CurrentUser.Value);
    }

    private void OnInit(EntityUid uid, RemoteControlConsoleComponent comp, ComponentInit args)
    {
        _link.EnsureSourcePorts(uid, comp.ConnectionPortId);
    }

    private void OnNewLink(EntityUid uid, RemoteControlConsoleComponent comp, NewLinkEvent args)
    {
        if (!_whitelist.CheckBoth(args.Sink, comp.Blacklist, comp.Whitelist))
            return;

        if (args.Source != uid || args.SourcePort != comp.ConnectionPortId)
            return;

        if (!HasComp<RemoteControllableComponent>(args.Sink))
            return;

        comp.LinkedEntities.Add(args.Sink);
    }

    private void OnPortDisconnected(EntityUid uid, RemoteControlConsoleComponent comp, PortDisconnectedEvent args)
    {
        if (args.Port == comp.ConnectionPortId)
            comp.LinkedEntities.Remove(args.RemovedPortUid);

        // the device link got severed while the turret was in use; either relink to another turret or kick the user out of the console.
        if(comp.CurrentUser is EntityUid user && TryComp<RemoteControllingComponent>(user, out var controlling) && controlling.ControlledEntity == args.RemovedPortUid)
        {
            _rc.EndRemoteControl(user);
            if (GetFirstValid(comp.LinkedEntities, comp.LastIndex) is EntityUid newTarget)
                _rc.RemoteControl(user, newTarget, uid);
        }
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
        _rc.RemoteControl(comp.CurrentUser.Value, target, uid);
    }

    private void OnUseInHand(EntityUid uid, RemoteControlConsoleComponent comp, UseInHandEvent args) => TryActivate(uid, comp, args.User);
    private void OnActivateInWorld(EntityUid uid, RemoteControlConsoleComponent comp, ActivateInWorldEvent args) => TryActivate(uid, comp, args.User);

    private void TryActivate(EntityUid uid, RemoteControlConsoleComponent comp, EntityUid user)
    {
        if (!this.IsPowered(uid, EntityManager))
            return;

        if (HasComp<RemoteControllingComponent>(user))
            return;

        if (comp.CurrentUser is not null)
            return; // in use

        if (comp.LinkedEntities.Count == 0)
            return; // none connected

        if (GetFirstValid(comp.LinkedEntities, comp.LastIndex) is not EntityUid target)
            return; // none available

        if (!_rc.RemoteControl(user, target, uid))
            return;

        comp.CurrentUser = user;
        comp.LastIndex = comp.LinkedEntities.IndexOf(target);

    }

    private void OnRCGetActionsEvent(EntityUid uid, RemoteControlConsoleComponent comp, RemoteControlInterfaceGetActionsEvent args)
    {
        if (comp.LinkedEntities.Count > 1)
        {
            if (!_actionContainer.EnsureAction(uid, ref comp.SwitchToNextActionEntity, comp.SwitchToNextAction))
                return;
            args.Actions.Add(comp.SwitchToNextActionEntity!.Value);
        }
    }


    private EntityUid? GetFirstValid(List<EntityUid> list, int startIndex, EntityUid? exclude = null)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int index = (i + startIndex) % list.Count;
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
