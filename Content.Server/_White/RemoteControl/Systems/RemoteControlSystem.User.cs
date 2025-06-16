using Content.Shared._White.RemoteControl.Components;
using Content.Shared.Mobs;

namespace Content.Server._White.RemoteControl.Systems;

public partial class RemoteControlSystem
{
    private void InitializeUser()
    {
        SubscribeLocalEvent<RemoteControlUserComponent, ComponentShutdown>(OnUserShutdown);

        SubscribeLocalEvent<RemoteControlUserComponent, MobStateChangedEvent>(OnUserMobStateChanged);
    }

    private void OnUserShutdown(EntityUid uid, RemoteControlUserComponent comp, ComponentShutdown args)
    {
        if (TryComp<RemoteControlTargetComponent>(comp.Target, out var targetComp) && targetComp.User == uid)
            EndRemoteControl((uid, comp), (comp.Target, targetComp));
    }

    private void OnUserMobStateChanged(EntityUid uid, RemoteControlUserComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
            EndRemoteControl((uid, comp));
    }
}
