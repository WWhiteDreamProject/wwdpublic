using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._White.Guns.ModularTurret;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._White.Guns.ModularTurret;

public sealed class ModularTurretSystem : SharedModularTurretSystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly ItemSlotsSystem _slot = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModularTurretComponent, ShotAttemptedEvent>(OnModularTurretShotAttempt);
        SubscribeLocalEvent<ModularTurretWeaponComponent, GunShotEvent>(OnModularTurretWeaponShot);
    }


    private void OnModularTurretShotAttempt(EntityUid turretUid, ModularTurretComponent comp, ref ShotAttemptedEvent args)
    {
        RechargeWeapon(turretUid, args.Used);
    }

    private void OnModularTurretWeaponShot(EntityUid gunUid, ModularTurretWeaponComponent comp, ref GunShotEvent args)
    {
        if(comp.CurrentTurretHolder is EntityUid turretUid)
            RechargeWeapon(turretUid, gunUid);
    }


    private void RechargeWeapon(EntityUid turretUid, EntityUid gunUid)
    {
        if (!HasComp<ModularTurretWeaponComponent>(gunUid))
            return;

        //var turretComp = Comp<ModularTurretComponent>(turretUid);
        if (!TryComp<BatteryComponent>(gunUid, out var gunBattery) || // || !turretComp.CanChargeWeapon
            !TryComp<BatteryComponent>(turretUid, out var turretBattery))
            return;

        float missing = gunBattery.MaxCharge - gunBattery.CurrentCharge;

        float recharged = -_battery.UseCharge(turretUid, missing, turretBattery);

        _battery.SetCharge(gunUid, gunBattery.CurrentCharge + recharged, gunBattery);
    }
}

