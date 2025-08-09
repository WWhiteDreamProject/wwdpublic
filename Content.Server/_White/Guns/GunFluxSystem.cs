using Content.Server._White.Knockdown;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Flash;
using Content.Server.Stunnable;
using Content.Server.Temperature.Systems;
using Content.Shared._Lavaland.Weapons.Ranged.Events;
using Content.Shared._Shitmed.Targeting;
using Content.Shared._White.Guns;
using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Content.Shared.Temperature;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Random;

namespace Content.Server._White.Guns;

public sealed class GunFluxSystem : SharedGunFluxSystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly StunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunFluxComponent, ProjectileShotEvent>(OnProjectileShot);
    }

    protected override void OnGunShot(EntityUid uid, GunFluxComponent comp, ref GunShotEvent args)
    {
        base.OnGunShot(uid, comp, ref args);
    }

    private void OnProjectileShot(EntityUid uid, GunFluxComponent comp, ProjectileShotEvent args)
    {
        if(!GetFluxCore(comp, out var core))
        {
            Log.Error($"{ToPrettyString(uid)} is missing its flux core right after being fired.");
            return;
        }

        if (!TryComp<ProjectileComponent>(args.FiredProjectile, out var proj))
            return;

        var curflux = GetCurrentFlux(core);
        float damageMul = Interp(core.BaseDamageMultiplier,
                                 core.BaseDamageMultiplierFlux ?? 0,
                                 core.MaxDamageMultiplier,
                                 core.MaxDamageMultiplierFlux ?? core.Capacity,
                                 curflux);

        proj.Damage *= damageMul;
    }

    protected override void ApplyOverheatDamage(EntityUid shooter, float damage, string type, TargetBodyPart? bodyPart)
    {
        base.ApplyOverheatDamage(shooter, damage, type, bodyPart);
        DamageSpecifier damagespec = new DamageSpecifier() { DamageDict = new() { { type, damage } } };
        _damage.TryChangeDamage(shooter, damagespec, targetPart: bodyPart);
    }

    protected override void TryMalfunction(EntityUid shooter, EntityUid gun, GunFluxComponent gunFlux, FluxCoreComponent core)
    {
        var curflux = GetCurrentFlux(core);
        var malfchance = Interp(core.BaseMalfunctionChance,
                                core.BaseMalfunctionChanceFlux ?? 0,
                                core.MaxMalfunctionChance,
                                core.MaxMalfunctionChanceFlux ?? core.Capacity,
                                curflux);
        if(_rng.Prob(malfchance))
        {
            var malfunction = _rng.Pick(gunFlux.MalfunctionWeightedList);
            switch (malfunction) // eugh
            {
                case "flash":
                    MalfunctionFlash(shooter, gun, gunFlux, core);
                    return;
                case "firefailure":
                    MalfunctionFirefailure(shooter, gun, gunFlux, core);
                    return;
                case "eject":
                    MalfunctionEject(shooter, gun, gunFlux, core);
                    return;
                case "explosion":
                    MalfunctionExplosion(shooter, gun, gunFlux, core);
                    return;
            }
        }
    }

    private void MalfunctionFlash(EntityUid shooter, EntityUid gun, GunFluxComponent gunFlux, FluxCoreComponent core)
    {
        _popup.PopupEntity(Loc.GetString("gun-malfunction-flash"), gun);
        _flash.Flash(shooter, null, null, 1.75f, 0.8f);
    }

    private void MalfunctionFirefailure(EntityUid shooter, EntityUid gun, GunFluxComponent gunFlux, FluxCoreComponent core)
    {
        var gunComp = Comp<GunComponent>(gun);
#pragma warning disable RA0002
        gunComp.NextFire += TimeSpan.FromSeconds(_rng.NextDouble(1, 2.5));
#pragma warning restore RA0002
        _popup.PopupEntity(Loc.GetString("gun-malfunction-firefailure"), gun);
        Dirty(gun, gunComp);
    }

    private void MalfunctionEject(EntityUid shooter, EntityUid gun, GunFluxComponent gunFlux, FluxCoreComponent core)
    {
        if(gunFlux.Owner == core.Owner)
        {
            //_popup.PopupEntity(Loc.GetString("gun-malfunction-eject-empty"), gun);
            return; 
        }
        if (!_slots.TryEject(gun, gunFlux.CoreSlot, gun, out _))
            return;
        _throw.TryThrow(core.Owner, _rng.NextAngle().ToVec(), playSound: false, doSpin: false);
        _popup.PopupEntity(Loc.GetString("gun-malfunction-eject"), gun);
    }

    private void MalfunctionExplosion(EntityUid shooter, EntityUid gun, GunFluxComponent gunFlux, FluxCoreComponent core)
    {
        const float minSlope = 0.6f;
        const float maxSlope = 1.35f;
        float slope = float.Lerp(minSlope, maxSlope, GetCurrentFlux(core) / core.Capacity);
        const float maxIntensity = 200;
        const float radius = 2;
        string message = core.Owner == gunFlux.Owner ? "gun-malfunction-explosion-gun" : "gun-malfunction-explosion-core";
        // the gun may just fucking explode, hence we'll raise the popup on the shooter instead
        _popup.PopupEntity(Loc.GetString(message), shooter);
        _explosion.QueueExplosion(core.Owner,
                                 "Default",
                                 _explosion.RadiusToIntensity(radius, slope, maxIntensity),
                                 slope,
                                 maxIntensity,
                                 tileBreakScale: 0,
                                 canCreateVacuum: false,
                                 user: shooter);

        _flash.Flash(shooter, null, null, 1.5f, 1f, false, stunDuration: TimeSpan.FromSeconds(4)); // get fucked
        _stun.TrySlowdown(shooter, TimeSpan.FromSeconds(4), true, 0.8f, 0.8f);                    // lmao
        QueueDel(core.Owner);
    }

    private float Interp(float min, float minAt, float max, float maxAt, float value) =>
        Math.Clamp((max - min) / (maxAt - minAt) * (value - minAt) + min,
                    min, max);
}
