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
    protected override void OnLampInit(EntityUid uid, RegulatorLampComponent comp, ComponentInit args)
    {
        comp.SafeTemperature += 273.15f; // celcius in prototypes, kelvin at runtime
        comp.UnsafeTemperature += 273.15f; // celcius in prototypes, kelvin at runtime
        if (comp.SafeTemperature > comp.UnsafeTemperature)
        {
            Log.Warning($"Entity {ToPrettyString(uid)} has SafeTemperature bigger than UnsafeTemperature. (s={comp.SafeTemperature}, u={comp.UnsafeTemperature}) Resolving by swapping them around.");
            (comp.SafeTemperature, comp.UnsafeTemperature) = (comp.UnsafeTemperature, comp.SafeTemperature);
        }

        if (comp.SafeTemperature == comp.UnsafeTemperature)
        {
            Log.Error($"Entity {ToPrettyString(uid)} has equal SafeTemperature and UnsafeTemperature. (s={comp.SafeTemperature}, u={comp.UnsafeTemperature}) Resolving by increasing UnsafeTemperature by 0.01f.");
            comp.UnsafeTemperature += 0.01f;
        }
        Dirty(uid, comp);
    }

    protected override void OnGunInit(EntityUid uid, GunOverheatComponent comp, ComponentInit args)
    {
        comp.TemperatureLimit += 273.15f; // celcius in prototypes, kelvin at runtime
        comp.MaxSafetyTemperature += 273.15f; // celcius in prototypes, kelvin at runtime
        Dirty(uid, comp);
    }

    protected override void OnGunShot(EntityUid uid, GunOverheatComponent comp, ref GunShotEvent args)
    {
        CheckForBurnout(uid, comp, args.User);
        _temp.ForceChangeTemperature(uid, comp.CurrentTemperature + comp.HeatCost);
    }

    private void CheckForBurnout(EntityUid uid, GunOverheatComponent comp, EntityUid shooter)
    {
        if (!GetLamp(uid, out var lampComp, comp) || lampComp is null)
            return;

        float breakChance = GetLampBreakChance(comp.CurrentTemperature, comp.LampBreakChanceMultiplier, lampComp);
        if (_rng.Prob(breakChance))
        {
            BurnoutLamp(lampComp, shooter);
        }
    }
}
