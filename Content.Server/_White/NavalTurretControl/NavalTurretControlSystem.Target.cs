using Content.Server.Chat.Systems;
using Content.Shared._White.NavalTurretControl;
using Content.Shared._White.RemoteControl;
using Content.Shared._White.RemoteControl.Components;
using Content.Shared.Mobs;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server._White.NavalTurretControl;

public partial class NavalTurretControlSystem
{
    private void InitializeTarget()
    {
        SubscribeLocalEvent<NavalTurretComponent, ComponentShutdown>(OnTargetShutdown);
        SubscribeLocalEvent<NavalTurretComponent, MobStateChangedEvent>(OnTargetMobStateChanged);
    }

    private void OnTargetShutdown(EntityUid uid, NavalTurretComponent comp, ComponentShutdown args)
    {
        if(comp.LinkedConsole is EntityUid consoleUid)
            UpdateState(consoleUid);
    }

    private void OnTargetMobStateChanged(EntityUid uid, NavalTurretComponent comp, MobStateChangedEvent args)
    {
        if(comp.LinkedConsole is EntityUid consoleUid)
            UpdateState(consoleUid);
    }
}
