using Content.Server.Temperature.Systems;
using Content.Shared._White.Guns;
using Content.Shared.Temperature;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Random;

namespace Content.Server._White.Guns;

public sealed class GunOverheatSystem : SharedGunOverheatSystem
{
    [Dependency] private readonly TemperatureSystem _temp = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunOverheatComponent, OnTemperatureChangeEvent>(OnTemperatureChange);
        SubscribeLocalEvent<RegulatorLampComponent, ComponentInit>(OnLampInit);
    }

    private void OnTemperatureChange(EntityUid uid, GunOverheatComponent comp, OnTemperatureChangeEvent args)
    {
        comp.CurrentTemperature = args.CurrentTemperature;
        DirtyField<GunOverheatComponent>(uid, nameof(GunOverheatComponent.CurrentTemperature));
    }

    private void OnLampInit(EntityUid uid, RegulatorLampComponent comp, ComponentInit args)
    {
        if (comp.SafeTemperature > comp.UnsafeTemperature)
        {
            Log.Warning(
                $"Entity {ToPrettyString(uid)} has SafeTemperature bigger than UnsafeTemperature. (s={comp.SafeTemperature}, u={comp.UnsafeTemperature}) Resolving by swapping them around.");
            (comp.SafeTemperature, comp.UnsafeTemperature) = (comp.UnsafeTemperature, comp.SafeTemperature);
            Dirty(uid, comp);
        }

        if (comp.SafeTemperature != comp.UnsafeTemperature)
            return;

        Log.Error(
            $"Entity {ToPrettyString(uid)} has equal SafeTemperature and UnsafeTemperature. (s={comp.SafeTemperature}, u={comp.UnsafeTemperature}) Resolving by increasing UnsafeTemperature by 0.01f.");
        comp.UnsafeTemperature += 0.01f;
        Dirty(uid, comp);
    }

    protected override void OnGunShot(Entity<GunOverheatComponent> gun, ref GunShotEvent args)
    {
        CheckForBurnout(gun, args.User);
        _temp.ForceChangeTemperature(gun, gun.Comp.CurrentTemperature + gun.Comp.HeatCost);
    }

    private void CheckForBurnout(Entity<GunOverheatComponent> gun, EntityUid shooter)
    {
        if (!GetLamp(gun, out var lamp))
            return;

        var breakChance = GetLampBreakChance(gun.Comp.CurrentTemperature, lamp, gun.Comp.LampBreakChanceMultiplier);
        if (Rng.Prob(breakChance))
            BurnoutLamp(lamp.Value, shooter);
    }
}
