using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Timing;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeBattery()
    {
        base.InitializeBattery();
        // Hitscan
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, AmmoCounterControlEvent>(OnControl);
        //SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, UpdateAmmoCounterEvent>(OnAmmoCountUpdate); // WWDP EDIT

        // Projectile
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, AmmoCounterControlEvent>(OnControl);
        //SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, UpdateAmmoCounterEvent>(OnAmmoCountUpdate); // WWDP EDIT
    }

    // WWDP EDIT - DEFUNCT - Trying to make gun-related status controls update themselves instead of whatever the fuck is currently going on
    //private void OnAmmoCountUpdate(EntityUid uid, BatteryAmmoProviderComponent component, UpdateAmmoCounterEvent args)
    //{
    //    if (args.Control is not BoxesStatusControl boxes) return;
    //    
    //    boxes.Update(component.Shots, component.Capacity);
    //}

    private void OnControl(EntityUid uid, BatteryAmmoProviderComponent component, AmmoCounterControlEvent args)
    {
        args.Control = new EnergyGunBatteryStatusControl(uid, component); // WWDP EDIT
    }
}
