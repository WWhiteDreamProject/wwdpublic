using Content.Server.Chat.Systems;
using Content.Shared._White.NavalTurretControl;
using Content.Shared._White.RemoteControl;
using Content.Shared._White.RemoteControl.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.Mobs;
using Content.Shared.Power;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server._White.NavalTurretControl;

public partial class NavalTurretControlSystem
{
    private void InitializeTarget()
    {
        SubscribeLocalEvent<NavalTurretComponent, ComponentShutdown>(OnTargetShutdown);
        SubscribeLocalEvent<NavalTurretComponent, MobStateChangedEvent>(OnTargetMobStateChanged);
        SubscribeLocalEvent<NavalTurretComponent, PowerChangedEvent>(OnTurretPowerChanged);
    }

    private void OnTurretShutdown(EntityUid uid, NavalTurretComponent comp, ComponentShutdown args)
    {
        UpdateStateForAllConnected(uid);
    }

    private void OnTurretMobStateChanged(EntityUid uid, NavalTurretComponent comp, MobStateChangedEvent args)
    {
        UpdateStateForAllConnected(uid);
    }

    private void OnTurretPowerChanged(EntityUid uid, NavalTurretComponent comp, PowerChangedEvent args)
    {
        UpdateStateForAllConnected(uid);
    }

    protected override void OnRenameVerb(ICommonSession player, EntityUid target, NavalTurretComponent comp)
    {
        var currentName = comp.Name;
        _quickDialog.OpenDialog(player, "Change Turret ID", ("Turret ID", currentName), (string newName) =>
        {
            comp.Name = newName;
            Dirty(target, comp);
        });
    }
}
