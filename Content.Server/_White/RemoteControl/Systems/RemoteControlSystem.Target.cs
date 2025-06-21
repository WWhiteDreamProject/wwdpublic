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
        SubscribeLocalEvent<RemoteControlTargetComponent, MapInitEvent>(OnTargetMapInit);
        SubscribeLocalEvent<RemoteControlTargetComponent, ComponentShutdown>(OnTargetShutdown);

        SubscribeLocalEvent<RemoteControlTargetComponent, SpeechSourceOverrideEvent>(OnTargetSpeechSourceOverride);

        SubscribeLocalEvent<RemoteControlTargetComponent, GetVerbsEvent<AlternativeVerb>>(GetAltVerb);

        SubscribeLocalEvent<RemoteControlTargetComponent, MobStateChangedEvent>(OnTargetMobStateChanged);

        SubscribeLocalEvent<RemoteControlTargetComponent, RemoteControlExitActionEvent>(OnExitAction);
    }

    private void OnTargetMapInit(EntityUid uid, RemoteControlTargetComponent comp, MapInitEvent args)
    {
        EntityUid? actionUid = null;
        _action.AddAction(uid, ref actionUid, comp.EndRemoteControlAction);

        if (actionUid.HasValue)
            comp.EndRemoteControlActionUid = actionUid.Value;
    }

    private void OnTargetShutdown(EntityUid uid, RemoteControlTargetComponent comp, ComponentShutdown args)
    {
        _action.RemoveAction(uid, comp.EndRemoteControlActionUid);

        if (!TryComp<RemoteControlUserComponent>(comp.User, out var userComponent))
            return;

        if (TryComp<RemoteControlConsoleComponent>(userComponent.Console, out var consoleComponent))
            consoleComponent.LinkedEntities.Remove(uid);

        EndRemoteControl((comp.User.Value, userComponent), (uid, comp), true);
    }

    private void OnTargetSpeechSourceOverride(EntityUid uid, RemoteControlTargetComponent comp, SpeechSourceOverrideEvent args)
    {
        if (comp.User is { } user)
            args.Override = user;
    }

    private void GetAltVerb(EntityUid uid, RemoteControlTargetComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!component.CanManually || !args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
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


    private void OnExitAction(EntityUid uid, RemoteControlTargetComponent component, RemoteControlExitActionEvent args)
    {
        if(component.User is not null)
            EndRemoteControl(component.User.Value, (uid, component));
    }

    private void OnTargetMobStateChanged(EntityUid uid, RemoteControlTargetComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive && component.User.HasValue)
            EndRemoteControl(component.User.Value, (uid, component), true);
    }
}
