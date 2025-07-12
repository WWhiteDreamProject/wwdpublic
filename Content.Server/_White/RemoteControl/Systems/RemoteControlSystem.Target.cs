using Content.Server.Chat.Systems;
using Content.Shared._White.RemoteControl;
using Content.Shared._White.RemoteControl.Components;
using Content.Shared.Mobs;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server._White.RemoteControl.Systems;

public partial class RemoteControlSystem
{
    private void InitializeTarget()
    {
        SubscribeLocalEvent<RemoteControllableComponent, MapInitEvent>(OnTargetMapInit);
        SubscribeLocalEvent<RemoteControllableComponent, ComponentShutdown>(OnTargetShutdown);

        SubscribeLocalEvent<RemoteControllableComponent, SpeechSourceOverrideEvent>(OnTargetSpeechSourceOverride);

        SubscribeLocalEvent<RemoteControllableComponent, GetVerbsEvent<AlternativeVerb>>(GetAltVerb);

        SubscribeLocalEvent<RemoteControllableComponent, MobStateChangedEvent>(OnTargetMobStateChanged);

        SubscribeLocalEvent<RemoteControllableComponent, RemoteControlExitActionEvent>(OnExitAction);
    }

    private void OnTargetMapInit(EntityUid uid, RemoteControllableComponent comp, MapInitEvent args)
    {
        EntityUid? actionUid = null;
        _action.AddAction(uid, ref actionUid, comp.EndRemoteControlAction);

        if (actionUid.HasValue)
            comp.EndRemoteControlActionUid = actionUid.Value;
    }

    private void OnTargetShutdown(EntityUid uid, RemoteControllableComponent comp, ComponentShutdown args)
    {
        _action.RemoveAction(uid, comp.EndRemoteControlActionUid);

        if (!TryComp<RemoteControllingComponent>(comp.User, out var userComponent))
            return;

        if (TryComp<RemoteControlConsoleComponent>(userComponent.Console, out var consoleComponent))
            consoleComponent.LinkedEntities.Remove(uid);

        EndRemoteControl((comp.User.Value, userComponent), (uid, comp), true);
    }

    private void OnTargetSpeechSourceOverride(EntityUid uid, RemoteControllableComponent comp, SpeechSourceOverrideEvent args)
    {
        if (comp.User is { } user)
            args.Override = user;
    }

    private void GetAltVerb(EntityUid uid, RemoteControllableComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!component.ManualControl || !args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        args.Verbs.Add(
            new AlternativeVerb
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

                    if (component.User is not null)
                    {
                        _popup.PopupEntity(Loc.GetString("manual-control-already-controlled"), uid, args.User);
                        return;
                    }

                    RemoteControl(args.User, (uid, component), uid);
                },
                Priority = -1
            });
    }


    private void OnExitAction(EntityUid uid, RemoteControllableComponent component, RemoteControlExitActionEvent args)
    {
        if(component.User is not null)
            EndRemoteControl(component.User.Value, (uid, component));
    }

    private void OnTargetMobStateChanged(EntityUid uid, RemoteControllableComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive && component.User.HasValue)
            EndRemoteControl(component.User.Value, (uid, component), true);
    }
}
