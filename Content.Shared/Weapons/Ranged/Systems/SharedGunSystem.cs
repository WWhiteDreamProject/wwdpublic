using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.CombatMode;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Contests;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Gravity;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Mech.Components; // Goobstation
using Content.Shared.MouseRotator;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem : EntitySystem
{
    [Dependency] private   readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] private   readonly INetManager _netManager = default!;
    [Dependency] protected readonly IPrototypeManager ProtoManager = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] protected readonly ISharedAdminLogManager Logs = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] protected readonly ExamineSystemShared Examine = default!;
    [Dependency] private   readonly ItemSlotsSystem _slots = default!;
    [Dependency] private   readonly RechargeBasicEntityAmmoSystem _recharge = default!;
    [Dependency] protected readonly SharedActionsSystem Actions = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private   readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] protected readonly SharedContainerSystem Containers = default!;
    [Dependency] private   readonly SharedGravitySystem _gravity = default!;
    [Dependency] protected readonly SharedPointLightSystem Lights = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] protected readonly SharedProjectileSystem Projectiles = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] protected readonly TagSystem TagSystem = default!;
    [Dependency] protected readonly ThrowingSystem ThrowingSystem = default!;
    [Dependency] private   readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly ContestsSystem _contests = default!; // WWDP

    private const float InteractNextFire = 0.3f;
    private const double SafetyNextFire = 0.5;
    private const float EjectOffset = 0.4f;
    protected const string AmmoExamineColor = "yellow";
    public const string ModeExamineColor = "crimson"; // WWDP examine
    public const string ModeExamineBadColor = "pink"; // WWDP examine

    public override void Initialize()
    {
        SubscribeAllEvent<RequestShootEvent>(OnShootRequest);
        SubscribeAllEvent<RequestStopShootEvent>(OnStopShootRequest);
        SubscribeLocalEvent<GunComponent, MeleeHitEvent>(OnGunMelee);

        // Ammo providers
        InitializeBallistic();
        InitializeBattery();
        InitializeCartridge();
        InitializeChamberMagazine();
        InitializeMagazine();
        InitializeRevolver();
        InitializeBasicEntity();
        InitializeClothing();
        InitializeContainer();
        InitializeSolution();

        // Interactions
        SubscribeLocalEvent<GunComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
        SubscribeLocalEvent<GunComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<GunComponent, CycleModeEvent>(OnCycleMode);
        SubscribeLocalEvent<GunComponent, HandSelectedEvent>(OnGunSelected);
        SubscribeLocalEvent<GunComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GunComponent, HolderMoveEvent>(OnHolderMove); // WWDP
    }

	// WWDP EDIT START
    private void OnHolderMove(EntityUid uid, GunComponent comp, ref HolderMoveEvent _args)
    {
        if (Timing.ApplyingState)
            return;
        MoveEvent args = _args.Ev;
        double posDiff = 0;
        if (!args.ParentChanged)
            posDiff = (args.OldPosition.Position - args.NewPosition.Position).Length();
        double rotDiff = Math.Abs(Angle.ShortestDistance(args.NewRotation, args.OldRotation).Degrees);

        UpdateBonusAngles(Timing.CurTime, comp, posDiff * comp.BonusAngleIncreaseMove + rotDiff * comp.BonusAngleIncreaseTurn);
        Dirty(uid, comp);
    }

    /// <summary>
    /// For "proper" recoil prediction
    /// </summary>
    protected void UpdateAngles(TimeSpan curTime, GunComponent component, double angleIncrease = 0)
    {
        var timeSinceLastFire = (curTime - component.CurrentAngleLastUpdate).TotalSeconds;
        // Two clamps, because first we need to compute how much CurrentAngle has decreased since the last time we fired
        // If we ignore the first clamp, CurrentAngle may "go" into negatives, making the first shot after a while have less or even no inaccuracy
        var oldTheta = MathHelper.Clamp(component.CurrentAngle - component.AngleDecayModified * timeSinceLastFire, component.MinAngleModified, component.MaxAngleModified);
        var newTheta = MathHelper.Clamp(oldTheta + angleIncrease, component.MinAngleModified, component.MaxAngleModified.Theta);
        component.CurrentAngle = new Angle(newTheta);
        component.CurrentAngleLastUpdate = curTime;

    }

    /// <summary>
    /// For "proper" recoil prediction
    /// </summary>
    /// <param name="curTime"></param>
    /// <param name="component"></param>
    protected void UpdateBonusAngles(TimeSpan curTime, GunComponent component, double angleIncrease = 0)
    {
        var timeSinceBonusUpdate = (curTime - component.BonusAngleLastUpdate).TotalSeconds;
        component.BonusAngle = MathHelper.Clamp(component.BonusAngle + angleIncrease - component.BonusAngleDecayModified * timeSinceBonusUpdate, 0, component.MaxBonusAngleModified);
        component.BonusAngleLastUpdate = curTime;
    }
	// WWDP EDIT END

    private void OnMapInit(Entity<GunComponent> gun, ref MapInitEvent args)
    {
#if DEBUG
        if (gun.Comp.NextFire > Timing.CurTime)
            Log.Warning($"Initializing a map that contains an entity that is on cooldown. Entity: {ToPrettyString(gun)}");

        DebugTools.Assert((gun.Comp.AvailableModes & gun.Comp.SelectedMode) != 0x0);
#endif

        RefreshModifiers((gun, gun));
    }

    private void OnGunMelee(EntityUid uid, GunComponent component, MeleeHitEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(uid, out var melee))
            return;

        if (melee.NextAttack > component.NextFire)
        {
            component.NextFire = Timing.CurTime + TimeSpan.FromSeconds(1f / component.FireRateModified); // WWDP delay based on the gun not melee so its shorter
            Dirty(uid, component);
        }
    }

    private void OnShootRequest(RequestShootEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null ||
            !_combatMode.IsInCombatMode(user))
            return;

        if (TryComp<MechPilotComponent>(user.Value, out var mechPilot))
            user = mechPilot.Mech;

        if (!TryGetGun(user.Value, out var ent, out var gun) ||
            HasComp<ItemComponent>(user))
            return;

        if (ent != GetEntity(msg.Gun))
            return;

        gun.ShootCoordinates = GetCoordinates(msg.Coordinates);
        gun.Target = GetEntity(msg.Target);
        AttemptShoot(user.Value, ent, gun);
    }

    private void OnStopShootRequest(RequestStopShootEvent ev, EntitySessionEventArgs args)
    {
        var gunUid = GetEntity(ev.Gun);

        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        if (TryComp<MechPilotComponent>(user.Value, out var mechPilot))
            user = mechPilot.Mech;

        if (!TryGetGun(user.Value, out var ent, out var gun))
            return;

        if (ent != gunUid)
            return;

        StopShooting(gunUid, gun);
    }

    public bool CanShoot(GunComponent component)
    {
        if (component.NextFire > Timing.CurTime)
            return false;

        return true;
    }

    public bool TryGetGun(EntityUid entity, out EntityUid gunEntity, [NotNullWhen(true)] out GunComponent? gunComp)
    {
        gunEntity = default;
        gunComp = null;

        if (TryComp<MechComponent>(entity, out var mech) &&
            mech.CurrentSelectedEquipment.HasValue &&
            TryComp<GunComponent>(mech.CurrentSelectedEquipment.Value, out var mechGun))
        {
            gunEntity = mech.CurrentSelectedEquipment.Value;
            gunComp = mechGun;
            return true;
        }

        if (EntityManager.TryGetComponent(entity, out HandsComponent? hands) &&
            hands.ActiveHandEntity is { } held &&
            TryComp(held, out GunComponent? gun))
        {
            gunEntity = held;
            gunComp = gun;
            return true;
        }

        // Last resort is check if the entity itself is a gun.
        if (TryComp(entity, out gun))
        {
            gunEntity = entity;
            gunComp = gun;
            return true;
        }

        return false;
    }

    private void StopShooting(EntityUid uid, GunComponent gun)
    {
        if (gun.ShotCounter == 0)
            return;

        gun.ShotCounter = 0;
        gun.ShootCoordinates = null;
        gun.Target = null;
        Dirty(uid, gun);
    }

    /// <summary>
    /// Attempts to shoot at the target coordinates. Resets the shot counter after every shot.
    /// </summary>
    public void AttemptShoot(EntityUid user, EntityUid gunUid, GunComponent gun, EntityCoordinates toCoordinates)
    {
        gun.ShootCoordinates = toCoordinates;
        AttemptShoot(user, gunUid, gun);
        gun.ShotCounter = 0;
    }

    /// <summary>
    /// Shoots by assuming the gun is the user at default coordinates.
    /// </summary>
    public void AttemptShoot(EntityUid gunUid, GunComponent gun)
    {
        var coordinates = new EntityCoordinates(gunUid, gun.DefaultDirection);
        gun.ShootCoordinates = coordinates;
        AttemptShoot(gunUid, gunUid, gun);
        gun.ShotCounter = 0;
    }

    private void AttemptShoot(EntityUid user, EntityUid gunUid, GunComponent gun)
    {
        if (gun.FireRateModified <= 0f ||
            !_actionBlockerSystem.CanAttack(user))
            return;

        var toCoordinates = gun.ShootCoordinates;

        if (toCoordinates == null)
            return;

        var curTime = Timing.CurTime;

        // check if anything wants to prevent shooting
        var prevention = new ShotAttemptedEvent
        {
            User = user,
            Used = (gunUid, gun)
        };
        RaiseLocalEvent(gunUid, ref prevention);
        if (prevention.Cancelled)
            return;

        RaiseLocalEvent(user, ref prevention);
        if (prevention.Cancelled)
            return;

        // Need to do this to play the clicking sound for empty automatic weapons
        // but not play anything for burst fire.
        if (gun.NextFire > curTime)
            return;

        var fireRate = TimeSpan.FromSeconds(1f / gun.FireRateModified);

        if (gun.SelectedMode == SelectiveFire.Burst || gun.BurstActivated)
            fireRate = TimeSpan.FromSeconds(1f / gun.BurstFireRate);

        // First shot
        // Previously we checked shotcounter but in some cases all the bullets got dumped at once
        // curTime - fireRate is insufficient because if you time it just right you can get a 3rd shot out slightly quicker.
        if (gun.NextFire < curTime - fireRate || gun.ShotCounter == 0 && gun.NextFire < curTime)
            gun.NextFire = curTime;

        var shots = 0;
        var lastFire = gun.NextFire;

        while (gun.NextFire <= curTime)
        {
            gun.NextFire += fireRate;
            shots++;
        }

        // NextFire has been touched regardless so need to dirty the gun.
        Dirty(gunUid, gun);

        // Get how many shots we're actually allowed to make, due to clip size or otherwise.
        // Don't do this in the loop so we still reset NextFire.
        if (!gun.BurstActivated)
        {
            switch (gun.SelectedMode)
            {
                case SelectiveFire.SemiAuto:
                    shots = Math.Min(shots, 1 - gun.ShotCounter);
                    break;
                case SelectiveFire.Burst:
                    shots = Math.Min(shots, gun.ShotsPerBurstModified - gun.ShotCounter);
                    break;
                case SelectiveFire.FullAuto:
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"No implemented shooting behavior for {gun.SelectedMode}!");
            }
        } else
            shots = Math.Min(shots, gun.ShotsPerBurstModified - gun.ShotCounter);

        var attemptEv = new AttemptShootEvent(user, null);
        RaiseLocalEvent(gunUid, ref attemptEv);

        if (attemptEv.Cancelled)
        {
            if (attemptEv.Message != null)
                PopupSystem.PopupClient(attemptEv.Message, gunUid, user);

            gun.BurstActivated = false;
            gun.BurstShotsCount = 0;
            gun.NextFire = TimeSpan.FromSeconds(Math.Max(lastFire.TotalSeconds + SafetyNextFire, gun.NextFire.TotalSeconds));
            return;
        }

        var fromCoordinates = Transform(user).Coordinates;
        // Remove ammo
        var ev = new TakeAmmoEvent(shots, new List<(EntityUid? Entity, IShootable Shootable)>(), fromCoordinates, user);

        // Listen it just makes the other code around it easier if shots == 0 to do this.
        if (shots > 0)
            RaiseLocalEvent(gunUid, ev);

        DebugTools.Assert(ev.Ammo.Count <= shots);
        DebugTools.Assert(shots >= 0);
        UpdateAmmoCount(gunUid);

        // Even if we don't actually shoot update the ShotCounter. This is to avoid spamming empty sounds
        // where the gun may be SemiAuto or Burst.
        gun.ShotCounter += shots;

        if (ev.Ammo.Count <= 0)
        {
            // triggers effects on the gun if it's empty
            var emptyGunShotEvent = new OnEmptyGunShotEvent();
            RaiseLocalEvent(gunUid, ref emptyGunShotEvent);

            gun.BurstActivated = false;
            gun.BurstShotsCount = 0;
            gun.NextFire += TimeSpan.FromSeconds(gun.BurstCooldown);

            // Play empty gun sounds if relevant
            // If they're firing an existing clip then don't play anything.
            if (shots > 0)
            {
                if (ev.Reason != null && Timing.IsFirstTimePredicted)
                {
                    PopupSystem.PopupCursor(ev.Reason);
                }

                // Don't spam safety sounds at gun fire rate, play it at a reduced rate.
                // May cause prediction issues? Needs more tweaking
                gun.NextFire = TimeSpan.FromSeconds(Math.Max(lastFire.TotalSeconds + SafetyNextFire, gun.NextFire.TotalSeconds));
                Audio.PlayPredicted(gun.SoundEmpty, gunUid, user);
                return;
            }

            return;
        }

        // Handle burstfire
        if (gun.SelectedMode == SelectiveFire.Burst)
        {
            gun.BurstActivated = true;
        }
        if (gun.BurstActivated)
        {
            gun.BurstShotsCount += shots;
            if (gun.BurstShotsCount >= gun.ShotsPerBurstModified)
            {
                gun.NextFire += TimeSpan.FromSeconds(gun.BurstCooldown);
                gun.BurstActivated = false;
                gun.BurstShotsCount = 0;
            }
        }
        UpdateAngles(curTime, gun); // WWDP
        // Shoot confirmed - sounds also played here in case it's invalid (e.g. cartridge already spent).
        Shoot(
            gunUid,
            gun,
            ev.Ammo,
            fromCoordinates,
            toCoordinates.Value,
            out var userImpulse,
            user,
            throwItems: attemptEv.ThrowItems);
        var shotEv = new GunShotEvent(user, ev.Ammo);
        RaiseLocalEvent(gunUid, ref shotEv);

        if (userImpulse && TryComp<PhysicsComponent>(user, out var userPhysics))
        {
            if (_gravity.IsWeightless(user, userPhysics))
                CauseImpulse(fromCoordinates, toCoordinates.Value, user, userPhysics);
        }
        UpdateAngles(curTime, gun, gun.AngleIncreaseModified); // WWDP
        Dirty(gunUid, gun);
    }

    public void Shoot(
        EntityUid gunUid,
        GunComponent gun,
        EntityUid ammo,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        out bool userImpulse,
        EntityUid? user = null,
        bool throwItems = false)
    {
        var shootable = EnsureShootable(ammo);
        Shoot(gunUid, gun, new List<(EntityUid? Entity, IShootable Shootable)>(1) { (ammo, shootable) }, fromCoordinates, toCoordinates, out userImpulse, user, throwItems);
    }

    public abstract void Shoot(
        EntityUid gunUid,
        GunComponent gun,
        List<(EntityUid? Entity, IShootable Shootable)> ammo,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        out bool userImpulse,
        EntityUid? user = null,
        bool throwItems = false);

    public void ShootProjectile(EntityUid uid, Vector2 direction, Vector2 gunVelocity, EntityUid gunUid, EntityUid? user = null, float speed = 20f)
    {
        var physics = EnsureComp<PhysicsComponent>(uid);
        Physics.SetBodyStatus(uid, physics, BodyStatus.InAir);

        var targetMapVelocity = gunVelocity + direction.Normalized() * speed;
        var currentMapVelocity = Physics.GetMapLinearVelocity(uid, physics);
        var finalLinear = physics.LinearVelocity + targetMapVelocity - currentMapVelocity;
        Physics.SetLinearVelocity(uid, finalLinear, body: physics);

        var projectile = EnsureComp<ProjectileComponent>(uid);
        Projectiles.SetShooter(uid, projectile, user ?? gunUid);
        projectile.Weapon = gunUid;

        TransformSystem.SetWorldRotation(uid, direction.ToWorldAngle() + projectile.Angle);
    }

    /// <summary>
    /// WWDP - Manually sets the targeted entity of the gun.
    /// Used for NPCs
    /// </summary>
    public void SetTarget(GunComponent gun, EntityUid target)
    {
        gun.Target = target;
    }

    protected abstract void Popup(string message, EntityUid? uid, EntityUid? user);

    /// <summary>
    /// Call this whenever the ammo count for a gun changes.
    /// </summary>
    protected virtual void UpdateAmmoCount(EntityUid uid, bool prediction = true) {}

    protected void SetCartridgeSpent(EntityUid uid, CartridgeAmmoComponent cartridge, bool spent)
    {
        if (cartridge.Spent != spent)
            Dirty(uid, cartridge);

        cartridge.Spent = spent;
        Appearance.SetData(uid, AmmoVisuals.Spent, spent);
    }

    /// <summary>
    /// Drops a single cartridge / shell
    /// </summary>
    protected void EjectCartridge(
        EntityUid entity,
        Angle? angle = null,
        bool playSound = true,
        GunComponent? gunComp = null)
    {
        var throwingForce = 0.01f;
        var throwingSpeed = 5f;
        var ejectAngleOffset = 3.7f;
        if (gunComp is not null)
        {
            throwingForce = gunComp.EjectionForce;
            throwingSpeed = gunComp.EjectionSpeed;
            ejectAngleOffset = gunComp.EjectAngleOffset;
        }

        // TODO: Sound limit version.
        var offsetPos = Random.NextVector2(EjectOffset);
        var xform = Transform(entity);

        var coordinates = xform.Coordinates;
        coordinates = coordinates.Offset(offsetPos);

        TransformSystem.SetLocalRotation(entity, Random.NextAngle(), xform);
        TransformSystem.SetCoordinates(entity, xform, coordinates);
        if (angle is null)
            angle = Random.NextAngle();

        Angle ejectAngle = angle.Value;
        ejectAngle += ejectAngleOffset; // 212 degrees; casings should eject slightly to the right and behind of a gun
        ThrowingSystem.TryThrow(entity, ejectAngle.ToVec().Normalized() * throwingForce, throwingSpeed);

        if (playSound && TryComp(entity, out CartridgeAmmoComponent? cartridge))
        {
            Audio.PlayPvs(cartridge.EjectSound, entity, AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation).WithVolume(-1f));
        }
    }

    protected IShootable EnsureShootable(EntityUid uid)
    {
        if (TryComp<CartridgeAmmoComponent>(uid, out var cartridge))
            return cartridge;

        return EnsureComp<AmmoComponent>(uid);
    }

    protected void RemoveShootable(EntityUid uid)
    {
        RemCompDeferred<CartridgeAmmoComponent>(uid);
        RemCompDeferred<AmmoComponent>(uid);
    }

    protected void MuzzleFlash(EntityUid gun, AmmoComponent component, Angle worldAngle, EntityUid? user = null)
    {
        var attemptEv = new GunMuzzleFlashAttemptEvent();
        RaiseLocalEvent(gun, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        var sprite = component.MuzzleFlash;

        if (sprite == null)
            return;

        var ev = new MuzzleFlashEvent(GetNetEntity(gun), sprite, worldAngle);
        CreateEffect(gun, ev, user);
    }

    public void CauseImpulse(EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, EntityUid user, PhysicsComponent userPhysics)
    {
        var fromMap = TransformSystem.ToMapCoordinates(fromCoordinates).Position;
        var toMap = TransformSystem.ToMapCoordinates(toCoordinates).Position;
        var shotDirection = (toMap - fromMap).Normalized();

        const float impulseStrength = 25.0f;
        var impulseVector =  shotDirection * impulseStrength;
        Physics.ApplyLinearImpulse(user, -impulseVector, body: userPhysics);
    }

    public void RefreshModifiers(Entity<GunComponent?> gun)
    {
        if (!Resolve(gun, ref gun.Comp))
            return;

        var comp = gun.Comp;
        var ev = new GunRefreshModifiersEvent(
            (gun, comp),
            comp.SoundGunshot,
            comp.CameraRecoilScalar,
            comp.AngleIncrease,
            comp.AngleDecay,
            comp.MaxAngle,
            comp.MinAngle,
            comp.BonusAngleDecay,	// WWDP EDIT
            comp.MaxBonusAngle,		// WWDP EDIT
            comp.ShotsPerBurst,
            comp.FireRate,
            comp.ProjectileSpeed
        );

        RaiseLocalEvent(gun, ref ev);

        comp.SoundGunshotModified = ev.SoundGunshot;
        comp.CameraRecoilScalarModified = ev.CameraRecoilScalar;
        comp.AngleIncreaseModified = ev.AngleIncrease;
        comp.AngleDecayModified = ev.AngleDecay;
        comp.MaxAngleModified = ClampAngle(ev.MaxAngle); 			// WWDP EDIT
        comp.MinAngleModified = ClampAngle(ev.MinAngle); 			// WWDP EDIT
        comp.BonusAngleDecayModified = ev.BonusAngleDecay;			// WWDP
        comp.MaxBonusAngleModified = ClampAngle(ev.MaxBonusAngle);	// WWDP
        comp.ShotsPerBurstModified = ev.ShotsPerBurst;
        comp.FireRateModified = ev.FireRate;
        comp.ProjectileSpeedModified = ev.ProjectileSpeed;

        Dirty(gun);

        Angle ClampAngle(Angle ang) => Math.Clamp(ang, 0, Math.Tau); // WWDP
    }

    // Goobstation
    public void SetTarget(EntityUid projectile,
        EntityUid? target,
        out TargetedProjectileComponent targeted,
        bool dirty = true)
    {
        targeted = EnsureComp<TargetedProjectileComponent>(projectile);
        targeted.Target = target;
        if (dirty)
            Dirty(projectile, targeted);
    }

    public void SetFireRate(GunComponent component, float fireRate) => component.FireRate = fireRate;

    public void SetUseKey(GunComponent component, bool useKey) => component.UseKey = useKey;

    public void SetSoundGunshot(GunComponent component, SoundSpecifier? sound) => component.SoundGunshot = sound;

    public void SetClumsyProof(GunComponent component, bool clumsyProof) => component.ClumsyProof = clumsyProof;

    protected abstract void CreateEffect(EntityUid gunUid, MuzzleFlashEvent message, EntityUid? user = null);

    /// <summary>
    /// Used for animated effects on the client.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class HitscanEvent : EntityEventArgs
    {
        public List<(NetCoordinates coordinates, Angle angle, SpriteSpecifier Sprite, float Distance)> Sprites = new();
    }
}

/// <summary>
///     Raised directed on the gun before firing to see if the shot should go through.
/// </summary>
/// <remarks>
///     Handling this in server exclusively will lead to mispredicts.
/// </remarks>
/// <param name="User">The user that attempted to fire this gun.</param>
/// <param name="Cancelled">Set this to true if the shot should be cancelled.</param>
/// <param name="ThrowItems">Set this to true if the ammo shouldn't actually be fired, just thrown.</param>
[ByRefEvent]
public record struct AttemptShootEvent(EntityUid User, string? Message, bool Cancelled = false, bool ThrowItems = false);

/// <summary>
///     Raised directed on the gun after firing.
/// </summary>
/// <param name="User">The user that fired this gun.</param>
[ByRefEvent]
public record struct GunShotEvent(EntityUid User, List<(EntityUid? Uid, IShootable Shootable)> Ammo);

public enum EffectLayers : byte
{
    Unshaded,
}

[Serializable, NetSerializable]
public enum AmmoVisuals : byte
{
    Spent,
    AmmoCount,
    AmmoMax,
    HasAmmo, // used for generic visualizers. c# stuff can just check ammocount != 0
    MagLoaded,
    BoltClosed,
}
