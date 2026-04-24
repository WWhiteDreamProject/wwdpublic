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

    private void OnTargetShutdown(EntityUid uid, NavalTurretComponent comp, ComponentShutdown args)
    {
        if (!TryComp<DeviceLinkSinkComponent>(uid, out var sink))
            return;

        foreach (var console in comp.LinkedConsoles)
        {
            // This currently nukes all connections between the turret and the consoles.
            // This is fine for now, as there are no possible connections between turret and console other than via the control port.
            // If such is added at some point in the future, DeviceLinkSystem will need a new method to
            // remove all connections for a specific port only.
            _link.RemoveSinkFromSource(console, uid, null, sink);
        }
        UpdateAllStates(comp);
    }

    private void OnTargetMobStateChanged(EntityUid uid, NavalTurretComponent comp, MobStateChangedEvent args)
    {
        UpdateAllStates(comp);
    }

    private void OnTurretPowerChanged(EntityUid uid, NavalTurretComponent comp, PowerChangedEvent args)
    {
        UpdateAllStates(comp);
    }
}
