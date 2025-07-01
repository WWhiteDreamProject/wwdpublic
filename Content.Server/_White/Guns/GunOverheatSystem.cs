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
        SubscribeLocalEvent<RegulatorLampComponent, ComponentInit>(OnLampInit);
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

        if (comp.SafeTemperature == comp.UnsafeTemperature)
        {
            Log.Error(
                $"Entity {ToPrettyString(uid)} has equal SafeTemperature and UnsafeTemperature. (s={comp.SafeTemperature}, u={comp.UnsafeTemperature}) Resolving by increasing UnsafeTemperature by 0.01f.");
            comp.UnsafeTemperature += 0.01f;
            Dirty(uid, comp);
        }
    }


    protected override void OnGunShot(EntityUid uid, GunOverheatComponent comp, ref GunShotEvent args)
    {
        base.OnGunShot(uid, comp, ref args);
        if (!GetLamp(uid, comp, out var lamp))
            return;

        var breakChance = GetLampBreakChance(GetCurrentTemperature(comp), lamp, comp.LampBreakChanceMultiplier);
        if (_rng.Prob(breakChance))
            BurnoutLamp(lamp, args.User);
    }
}
