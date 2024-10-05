using Content.Server.Power.EntitySystems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server._White.Guns;

public sealed class PoweredSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly GunSystem _gun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PoweredComponent, AttemptShootEvent>(OnShoot);
        SubscribeLocalEvent<PoweredComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
    }

    private void OnShoot(EntityUid uid, PoweredComponent component, AttemptShootEvent args)
    {
        _gun.RefreshModifiers(uid);
    }

    private void OnGunRefreshModifiers(EntityUid uid, PoweredComponent component, ref GunRefreshModifiersEvent args)
    {
        if (!_battery.TryGetBatteryComponent(uid, out var battery, out var batteryUid)
            || !_battery.TryUseCharge(batteryUid.Value, component.EnergyPerUse, battery))
            return;

        args.ProjectileSpeed += component.ProjectileSpeedModified;
    }
}
