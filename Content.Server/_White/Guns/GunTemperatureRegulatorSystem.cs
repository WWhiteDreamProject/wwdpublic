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
        SubscribeLocalEvent<GunOverheatComponent, OnTemperatureChangeEvent>(OnTemperatureChange);
    }

    private void OnTemperatureChange(EntityUid uid, GunOverheatComponent comp, OnTemperatureChangeEvent args)
    {
        comp.CurrentTemperature = args.CurrentTemperature;
        DirtyField<GunOverheatComponent>(uid, nameof(GunOverheatComponent.CurrentTemperature));
    }

    protected override void OnGunShot(EntityUid uid, GunOverheatComponent comp, ref GunShotEvent args)
    {
        CheckForBurnout(uid, comp, args.User);
        _temp.ForceChangeTemperature(uid, comp.CurrentTemperature + comp.HeatCost);
    }

    private void CheckForBurnout(EntityUid uid, GunOverheatComponent comp, EntityUid shooter)
    {
        if (!_slots.TryGetSlot(uid, comp.LampSlot, out var slot) ||
            !TryComp<RegulatorLampComponent>(slot.Item, out var lampComp) ||
            !lampComp.Intact)
            return;

        float breakChance = GetLampBreakChance(comp.CurrentTemperature, lampComp) * comp.LampBreakChanceMultiplier;
        if (_rng.Prob(breakChance))
        {
            BurnoutLamp(lampComp, shooter);
        }
    }
}
