using Content.Server.Chat.Systems;
using Content.Server.DeviceLinking.Systems;
using Content.Server.GameTicking;
using Content.Server.Power.EntitySystems;
using Content.Shared._White.Overlays;
using Content.Shared._White.RemoteControl;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Lock;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Server.Chat.Systems.ChatSystem;

namespace Content.Server._White.RemoteControl;

public sealed class RemoteControlSystem : EntitySystem
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
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly LockSystem _lock = default!;

    public const string SinkPortId = "RemoteControlSinkPort";
    public const string SourcePortId = "RemoteControlSourcePort";


    public override void Initialize()
    {
        SubscribeLocalEvent<ManuallyControllableComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);

        SubscribeLocalEvent<RemoteControllableComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<RemoteControllableComponent, GhostAttemptHandleEvent>(OnGhostAttempt);
        SubscribeLocalEvent<RemoteControllableComponent, MapInitEvent>(OnCompMapInit);
        SubscribeLocalEvent<RemoteControllableComponent, RemoteControlExitActionEvent>(OnEndRC);
        SubscribeLocalEvent<RemoteControllableComponent, SpeechSourceOverrideEvent>(OnSpeechSourceOverride);
        SubscribeLocalEvent<RemoteControllableComponent, MobStateChangedEvent>(OnControllableMobStateChanged);
        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandICChatRecipients);
        SubscribeLocalEvent<RemoteControllableComponent, LinkAttemptEvent>(OnLinkAttempt);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RemoteControllingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if(ShouldEndRC(uid, comp))
                EndRemoteControl(uid, false);
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

    private void OnAltVerb(EntityUid uid, ManuallyControllableComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!comp.Enabled || !args.CanAccess || !args.CanInteract || !args.CanComplexInteract ||
            !TryComp<RemoteControllableComponent>(uid, out var controllable))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/_White/Interface/VerbIcons/joystick.png")),
            Text = Loc.GetString("manual-control-verb"),
            Act = () =>
            {
                if (_lock.IsLocked(uid))
                {
                    _popup.PopupEntity(Loc.GetString("manual-control-locked"), uid, args.User);
                    return;
                }

                if (controllable.ControllingMind is not null)
                {
                    _popup.PopupEntity(Loc.GetString("manual-control-already-controlled"), uid, args.User);
                    return;
                }

                RemoteControl(args.User, uid, uid);
            },
            Priority = -1
        });
    }


    private void OnControllableMobStateChanged(EntityUid uid, RemoteControllableComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive && comp.ControllingEntity is EntityUid controller)
            EndRemoteControl(controller, true);
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
            EndRemoteControl(comp.ControllingEntity.Value, false);
    }

    private void OnCompInit(EntityUid uid, RemoteControllableComponent comp, ComponentInit args)
    {
        if(comp.EnsureSinkPort)
            _link.EnsureSinkPorts(uid, SinkPortId);
    }

    private void OnCompMapInit(EntityUid uid, RemoteControllableComponent comp, MapInitEvent args)
    {
        _action.AddAction(uid, ref comp.EndRemoteControlActionEntity, comp.EndRemoteControlAction);
    }

    private void OnLinkAttempt(EntityUid uid, RemoteControllableComponent comp, LinkAttemptEvent args)
    {
        if (args.SinkPort == SinkPortId && args.SourcePort != SourcePortId)
            args.Cancel();
    }

    public bool RemoteControl(EntityUid user, EntityUid target, EntityUid interfaceEntity, bool overlay = false, RemoteControllableComponent? controllable = null)
    {
        if (!Resolve(target, ref controllable))
            return false;

        if (HasComp<RemoteControllingComponent>(user))
            return false;

        // should i log these?
        if (_mind.GetMind(user) is not EntityUid userMind)
            return false;

        if (HasComp<VisitingMindComponent>(target))
            return false;

        DebugTools.Assert(controllable.ControllingMind is null);
        DebugTools.Assert(controllable.ControllingEntity is null);

        var controlling = EnsureComp<RemoteControllingComponent>(user);
        controlling.ControlledEntity = target;
        controlling.UsedInterface = interfaceEntity;

        controllable.ControllingEntity = user;
        controllable.ControllingMind = userMind;

        var ev = new RemoteControlInterfaceGetActionsEvent(user, target);
        RaiseLocalEvent(interfaceEntity, ev);
        foreach (var action in ev.Actions)
            _action.AddAction(target, action, interfaceEntity);

        var mindComp = Comp<MindComponent>(userMind);
        _mind.Visit(userMind, target, mindComp);
        DebugTools.Assert(mindComp.VisitingEntity == target);
        RaiseLocalEvent(interfaceEntity, new RemoteControlInterfaceStartEvent(user, target));

        if (overlay)
            EnsureComp<RemoteControlOverlayComponent>(target);
        else if (TryComp<RemoteControlOverlayComponent>(target, out var overlayComp))
            RemComp(target, overlayComp);

            return true;
    }

    public void EndRemoteControlNoEv(EntityUid user) => EndRemoteControl(user, out _);
    public void EndRemoteControl(EntityUid user, bool forced)
    {
        if (EndRemoteControl(user, out var interfaceEntity) && interfaceEntity is not null)
            RaiseLocalEvent(interfaceEntity.Value, new RemoteControlInterfaceEndEvent(forced));

    }

    private bool EndRemoteControl(EntityUid user, out EntityUid? interfaceEntity)
    {
        interfaceEntity = null;
        if (!TryComp<RemoteControllingComponent>(user, out var controlling))
            return false;

        if (!TryComp<RemoteControllableComponent>(controlling.ControlledEntity, out var rc))
            return false;

        if (_mind.GetMind(user) is not EntityUid userMind)
            return false;

        rc.ControllingEntity = null;
        rc.ControllingMind = null;
        interfaceEntity = controlling.UsedInterface;
        RemComp(user, controlling);
        _mind.UnVisit(userMind);
        return true;
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
        SubscribeLocalEvent<RemoteControlConsoleComponent, RemoteControlInterfaceGetActionsEvent>(OnRCGetActionsEvent);
        SubscribeLocalEvent<RemoteControlConsoleComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<RemoteControlConsoleComponent, RemoteControlInterfaceEndEvent>(OnRemoteControlEnd);
        SubscribeLocalEvent<RemoteControlConsoleComponent, RemoteControlInterfaceStartEvent>(OnRemoteControlStart);
    }

    private void OnRemoteControlEnd(EntityUid uid, RemoteControlConsoleComponent comp, RemoteControlInterfaceEndEvent args)
    {
        if (args.Forced && TrySwitchToNextAvailable(uid, comp))
            return;
        comp.CurrentUser = null;
        comp.CurrentEntity = null;
    }

    private void OnRemoteControlStart(EntityUid uid, RemoteControlConsoleComponent comp, RemoteControlInterfaceStartEvent args)
    {
        comp.CurrentUser = args.User;
        comp.CurrentEntity = args.Target;
        comp.LastIndex = comp.LinkedEntities.IndexOf(args.Target);

    }

    private void OnPowerChanged(EntityUid uid, RemoteControlConsoleComponent comp, PowerChangedEvent args)
    {
        // this will raise RemoteControlInterfaceEndEvent directed on the console, which is handled above
        // hence we don't need to clear comp.CurrentUser here
        if (!args.Powered && comp.CurrentUser is not null)
            _rc.EndRemoteControl(comp.CurrentUser.Value, false); 
    }

    private void OnInit(EntityUid uid, RemoteControlConsoleComponent comp, ComponentInit args)
    {
        _link.EnsureSourcePorts(uid, RemoteControlSystem.SourcePortId);
    }

    private void OnNewLink(EntityUid uid, RemoteControlConsoleComponent comp, NewLinkEvent args)
    {
        if (!_whitelist.CheckBoth(args.Sink, comp.Blacklist, comp.Whitelist))
            return;

        if (args.Source != uid || args.SourcePort != RemoteControlSystem.SourcePortId)
            return;

        if (args.SinkPort != RemoteControlSystem.SinkPortId)
            return;

        if (!HasComp<RemoteControllableComponent>(args.Sink))
            return;

        comp.LinkedEntities.Add(args.Sink);
    }

    private void OnPortDisconnected(EntityUid uid, RemoteControlConsoleComponent comp, PortDisconnectedEvent args)
    {
        if (args.Port == RemoteControlSystem.SourcePortId)
            comp.LinkedEntities.Remove(args.RemovedPortUid);

        // the device link got severed while the turret was in use; either relink to another turret or kick the user out of the console.
        if(comp.CurrentUser is EntityUid user && TryComp<RemoteControllingComponent>(user, out var controlling) && controlling.ControlledEntity == args.RemovedPortUid)
        {
            _rc.EndRemoteControl(user, true);
            //if (GetFirstValid(comp.LinkedEntities, comp.LastIndex) is EntityUid newTarget)
            //    _rc.RemoteControl(user, newTarget, uid);
        }
    }

    private void OnNextAction(EntityUid uid, RemoteControlConsoleComponent comp, RemoteControlConsoleSwitchNextActionEvent args)
    {
        TrySwitchToNextAvailable(uid, comp);
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

        if (GetFirstValid(comp) is not EntityUid target)
            return; // none available

        _rc.RemoteControl(user, target, uid, true);
    }

    private bool TrySwitchToNextAvailable(EntityUid console, RemoteControlConsoleComponent comp)
    {
        if (comp.CurrentUser is not EntityUid user)
            return false; // probably worth a warning in logs
        
        if (GetFirstValid(comp, comp.CurrentEntity) is not EntityUid target)
            return false; // no other targets available

        comp.LastIndex = comp.LinkedEntities.IndexOf(target);

        _rc.EndRemoteControl(user, false);
        return _rc.RemoteControl(user, target, console, true);
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

    private EntityUid? GetFirstValid(RemoteControlConsoleComponent comp, EntityUid? exclude = null) => GetFirstValid(comp.LinkedEntities, comp.LastIndex, exclude);
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

public sealed partial class RemoteControlInterfaceStartEvent(EntityUid user, EntityUid target) : EntityEventArgs
{
    public EntityUid User = user;
    public EntityUid Target = target;
}
public sealed partial class RemoteControlInterfaceEndEvent(bool forced) : EntityEventArgs
{
    /// <summary>
    /// If false, remote control was ended by user pressing the exit action.
    /// If true, remote control was ended by some other outside factor (e.g. the controlled mob dying or getting deleted)
    /// Really only used for automatically switching to a new turret if the current one is destroyed, so it probably should be renamed.
    /// </summary>
    public bool Forced = forced;
}
