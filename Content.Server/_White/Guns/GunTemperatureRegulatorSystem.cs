using Content.Server.Temperature.Systems;
using Content.Shared._White.Guns;
using Content.Shared.Temperature;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._White.Guns;

public sealed class GunTemperatureRegulatorSystem : SharedGunTemperatureRegulatorSystem
{
    [Dependency] private readonly TemperatureSystem _temp = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunTemperatureRegulatorComponent, OnTemperatureChangeEvent>(OnTemperatureChange);
    }

    private void OnTemperatureChange(EntityUid uid, GunTemperatureRegulatorComponent comp, OnTemperatureChangeEvent args)
    {
        comp.CurrentTemperature = args.CurrentTemperature;
        DirtyField<GunTemperatureRegulatorComponent>(uid, nameof(GunTemperatureRegulatorComponent.CurrentTemperature));
    }

    protected override void OnGunShot(EntityUid uid, GunTemperatureRegulatorComponent comp, ref GunShotEvent args)
    {
        CheckForBurnout(uid, comp, args.User);
        _temp.ForceChangeTemperature(uid, comp.CurrentTemperature + comp.HeatCost);
    }

    private void CheckForBurnout(EntityUid uid, GunTemperatureRegulatorComponent comp, EntityUid shooter)
    {
        if (!_slots.TryGetSlot(uid, comp.LampSlot, out var slot) ||
            !TryComp<GunRegulatorLampComponent>(slot.Item, out var lampComp) ||
            !lampComp.Intact)
            return;

        float breakChance = (comp.CurrentTemperature - lampComp.SafeTemperature) / lampComp.UnsafeTemperature * comp.LampBreakChanceMultiplier;
        if (breakChance <= 0)
            return;
        if (breakChance >= 1 || _rng.Prob(breakChance))
        {
            BurnoutLamp(lampComp, shooter);
        }
    }
}
