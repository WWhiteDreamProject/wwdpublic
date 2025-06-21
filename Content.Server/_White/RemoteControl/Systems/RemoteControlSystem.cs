using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Interaction;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared._White.Overlays;
using Content.Shared._White.RemoteControl.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.DeviceLinking;
using Content.Shared.Lock;
using Content.Shared.Mind.Components;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._White.RemoteControl.Systems;

public sealed partial class RemoteControlSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly InteractionSystem _interactionSystem = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public static ProtoId<SinkPortPrototype> SinkPortId = "RemoteControlOutputPort";
    public static ProtoId<SourcePortPrototype> SourcePortId = "RemoteControlInputPort";

    public override void Initialize()
    {
        InitializeConsole();
        InitializeTarget();
        InitializeUser();

        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandICChatRecipients);
    }

    // I hate it.
    private void OnExpandICChatRecipients(ExpandICChatRecipientsEvent args)
    {
        var sourceXform = Transform(args.Source);

        var query = EntityQueryEnumerator<RemoteControlTargetComponent, TransformComponent>();
        while (query.MoveNext(out _, out var comp, out var xform))
        {
            var range = xform.MapID != sourceXform.MapID
                ? -1
                : (_transform.GetWorldPosition(sourceXform) - _transform.GetWorldPosition(xform)).Length();

            if (range < 0 || range > args.VoiceRange)
                continue;

            if (TryComp<ActorComponent>(comp.User, out var actor))
                args.Recipients.TryAdd(actor.PlayerSession, new ChatSystem.ICChatRecipientData(range, false, false));
        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RemoteControlUserComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if(!_actionBlocker.CanInteract(uid, comp.Console)
                || !_interactionSystem.InRangeAndAccessible(uid, comp.Console))
                EndRemoteControl((uid, comp), comp.Target, comp.Console, true);
        }
    }

    private bool RemoteControl(Entity<RemoteControlUserComponent?> user, Entity<RemoteControlTargetComponent?> target, Entity<RemoteControlConsoleComponent?> console, bool overlay = false)
    {
        if (!Resolve(target, ref target.Comp)
            || HasComp<RemoteControlUserComponent>(user)
            || HasComp<VisitingMindComponent>(target)
            || _mind.GetMind(user) is not { } userMind)
            return false;

        user.Comp = EnsureComp<RemoteControlUserComponent>(user);
        user.Comp.Target = target;
        user.Comp.Console = console;

        target.Comp.User = user;

        if (Resolve(console, ref console.Comp, false))
        {
            console.Comp.User = user;
            console.Comp.Target = target;
            console.Comp.LastIndex = console.Comp.LinkedEntities.IndexOf(target);

            _action.AddAction(target, console.Comp.SwitchToNextActionUid, console);
        }

        _mind.Visit(userMind, target);

        if (overlay)
            EnsureComp<RemoteControlOverlayComponent>(target);
        else if (TryComp<RemoteControlOverlayComponent>(target, out var overlayComp))
            RemComp(target, overlayComp);

        return true;
    }

    private bool EndRemoteControl(Entity<RemoteControlUserComponent?> user, bool shouldAutoSwitch = false)
    {
        if (!Resolve(user, ref user.Comp))
            return false;

        return EndRemoteControl(user, user.Comp.Target, user.Comp.Console, shouldAutoSwitch);
    }

    private bool EndRemoteControl(
        Entity<RemoteControlUserComponent?> user,
        Entity<RemoteControlTargetComponent?> target,
        bool shouldAutoSwitch = false
        )
    {
        if (!Resolve(user, ref user.Comp) || !Resolve(target, ref target.Comp))
            return false;

        return EndRemoteControl(user, target, user.Comp.Console, shouldAutoSwitch);
    }

    private bool EndRemoteControl(
        Entity<RemoteControlUserComponent?> user,
        Entity<RemoteControlConsoleComponent?> console,
        bool shouldAutoSwitch = false
    )
    {
        if (!Resolve(user, ref user.Comp) || !Resolve(console, ref console.Comp))
            return false;

        return EndRemoteControl(user, user.Comp.Target, console, shouldAutoSwitch);
    }

    private bool EndRemoteControl(
        Entity<RemoteControlUserComponent?> user,
        Entity<RemoteControlTargetComponent?> target,
        Entity<RemoteControlConsoleComponent?> console,
        bool shouldAutoSwitch = false
        )
    {
        if (!Resolve(user, ref user.Comp)
            || !Resolve(target, ref target.Comp)
            || _mind.GetMind(user) is not { } userMind)
            return false;

        target.Comp.User = null;

        _mind.UnVisit(userMind);

        RemComp(user.Owner, user.Comp);

        if (!Resolve(console, ref console.Comp, false))
            return true;

        console.Comp.Target = null;

        _action.RemoveAction(target, console.Comp.SwitchToNextActionUid);

        if (shouldAutoSwitch && TrySwitchToNextAvailable(user.Comp.Console, console.Comp))
            return true;

        console.Comp.User = null;

        return true;
    }
}
