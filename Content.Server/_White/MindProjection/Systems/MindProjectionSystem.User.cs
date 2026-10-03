using Content.Shared._White.MindProjection.Components;
using Content.Shared.Mobs;

namespace Content.Server._White.MindProjection.Systems;

public partial class MindProjectionSystem
{
    private void InitializeUser()
    {
        SubscribeLocalEvent<MingProjectingComponent, ComponentShutdown>(OnUserShutdown);

        SubscribeLocalEvent<MingProjectingComponent, MobStateChangedEvent>(OnUserMobStateChanged);
    }

    private void OnUserShutdown(EntityUid uid, MingProjectingComponent comp, ComponentShutdown args)
    {
        if (TryComp<MindProjectionTargetComponent>(comp.Target, out var targetComp) && targetComp.User == uid)
            EndRemoteControl((uid, comp), (comp.Target, targetComp));
    }

    private void OnUserMobStateChanged(EntityUid uid, MingProjectingComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
            EndRemoteControl((uid, comp));
    }
}
