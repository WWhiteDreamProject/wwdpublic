using Content.Shared._White.RemoteControl.Components;
using Content.Shared.Mobs;

namespace Content.Server._White.RemoteControl.Systems;

public partial class RemoteControlSystem
{
    private void InitializeUser()
    {
        SubscribeLocalEvent<RemoteControllingComponent, ComponentShutdown>(OnUserShutdown);

        SubscribeLocalEvent<RemoteControllingComponent, MobStateChangedEvent>(OnUserMobStateChanged);
    }

    private void OnUserShutdown(EntityUid uid, RemoteControllingComponent comp, ComponentShutdown args)
    {
        if (TryComp<RemoteControllableComponent>(comp.Target, out var targetComp) && targetComp.User == uid)
            EndRemoteControl((uid, comp), (comp.Target, targetComp));
    }

    private void OnUserMobStateChanged(EntityUid uid, RemoteControllingComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
            EndRemoteControl((uid, comp));
    }
}
