using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Server.DeviceLinking.Systems;

public sealed partial class GunSignalControlSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunSignalControlComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<GunSignalControlComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(Entity<GunSignalControlComponent> gunControl, ref MapInitEvent args)
    {
        _signalSystem.EnsureSinkPorts(gunControl, gunControl.Comp.TriggerPort, gunControl.Comp.TogglePort, gunControl.Comp.OnPort, gunControl.Comp.OffPort);
    }

    // WWDP EDIT START
    private void OnSignalReceived(Entity<GunSignalControlComponent> gunControl, ref SignalReceivedEvent args)
    {
        if (!_gun.TryGetGun(gunControl, out var gunUid, out var gun)) 
            return;

        if (args.Port == gunControl.Comp.TriggerPort)
            _gun.AttemptShoot(gunControl.Owner, gunUid, gun);

        var autoShoot = EnsureComp<AutoShootGunComponent>(gunControl);

        if (args.Port == gunControl.Comp.TogglePort)
            _gun.SetEnabled(gunUid, autoShoot, !autoShoot.Enabled);

        if (args.Port == gunControl.Comp.OnPort)
            _gun.SetEnabled(gunUid, autoShoot, true);

        if (args.Port == gunControl.Comp.OffPort)
            _gun.SetEnabled(gunUid, autoShoot, false);
    }
    // WWDP EDIT END
}
