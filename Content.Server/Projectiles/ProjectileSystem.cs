using Content.Server.Administration.Logs;
using Content.Server.Effects;
using Content.Server.Hands.Systems;
using Content.Server.UserInterface;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._White.Penetrated;
using Content.Shared._White.Projectile;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Server.Projectiles;

public sealed class ProjectileSystem : SharedProjectileSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ColorFlashEffectSystem _color = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly GunSystem _guns = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    // WD EDIT START
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PenetratedSystem _penetrated = default!;
    // WD EDIT END

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnStartCollide);
        // WD EDIT START
        SubscribeLocalEvent<EmbeddableProjectileComponent, EmbedEvent>(OnEmbed);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ActivateInWorldEvent>(OnEmbedActivate, before: new[] {typeof(ActivatableUISystem)});
        SubscribeLocalEvent<EmbeddableProjectileComponent, RemoveEmbeddedProjectileEvent>(OnEmbedRemove);
        // WD EDIT END
    }

    private void OnStartCollide(EntityUid uid, ProjectileComponent component, ref StartCollideEvent args)
    {
        // This is so entities that shouldn't get a collision are ignored.
        if (args.OurFixtureId != ProjectileFixture || !args.OtherFixture.Hard
            || component.DamagedEntity || component is { Weapon: null, OnlyCollideWhenShot: true })
            return;

        var target = args.OtherEntity;
        // it's here so this check is only done once before possible hit
        var attemptEv = new ProjectileReflectAttemptEvent(uid, component, false);
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            SetShooter(uid, component, target);
            return;
        }

        var ev = new ProjectileHitEvent(component.Damage, target, component.Shooter);
        RaiseLocalEvent(uid, ref ev);

        var otherName = ToPrettyString(target);
        var direction = args.OurBody.LinearVelocity.Normalized();
        var modifiedDamage = _damageableSystem.TryChangeDamage(target, ev.Damage, component.IgnoreResistances, origin: component.Shooter);
        var deleted = Deleted(target);

        if (modifiedDamage is not null && EntityManager.EntityExists(component.Shooter))
        {
            if (modifiedDamage.Any() && !deleted)
            {
                _color.RaiseEffect(Color.Red, new List<EntityUid> { target }, Filter.Pvs(target, entityManager: EntityManager));
            }

            _adminLogger.Add(LogType.BulletHit,
                HasComp<ActorComponent>(target) ? LogImpact.Extreme : LogImpact.High,
                $"Projectile {ToPrettyString(uid):projectile} shot by {ToPrettyString(component.Shooter!.Value):user} hit {otherName:target} and dealt {modifiedDamage.GetTotal():damage} damage");
        }

        if (!deleted)
        {
            _guns.PlayImpactSound(target, modifiedDamage, component.SoundHit, component.ForceSound);
            _sharedCameraRecoil.KickCamera(target, direction);
        }

        component.DamagedEntity = true;

        if (component.DeleteOnCollide)
            QueueDel(uid);

        if (component.ImpactEffect != null && TryComp<TransformComponent>(uid, out var xform))
        {
            RaiseNetworkEvent(new ImpactEffectEvent(component.ImpactEffect, GetNetCoordinates(xform.Coordinates)), Filter.Pvs(xform.Coordinates, entityMan: EntityManager));
        }
    }

    // WD EDIT START
    private void OnEmbed(EntityUid uid, EmbeddableProjectileComponent component, ref EmbedEvent args)
    {
        var dmg = _damageable.TryChangeDamage(args.Embedded, component.Damage, origin: args.Shooter);
        if (dmg is { Empty: false })
            _color.RaiseEffect(Color.Red, new List<EntityUid>() { args.Embedded }, Filter.Pvs(args.Embedded, entityManager: EntityManager));
    }

    private void OnEmbedActivate(EntityUid uid, EmbeddableProjectileComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled
            || !AttemptEmbedRemove(uid, args.User, component))
            return;

        args.Handled = true;
    }

    private void OnEmbedRemove(EntityUid uid, EmbeddableProjectileComponent component, RemoveEmbeddedProjectileEvent args)
    {
        // Whacky prediction issues.
        if (args.Cancelled)
            return;

        if (component.DeleteOnRemove)
        {
            QueueDel(uid);
            FreePenetrated(uid);
            return;
        }

        var xform = Transform(uid);
        TryComp<PhysicsComponent>(uid, out var physics);
        _physics.SetBodyType(uid, BodyType.Dynamic, body: physics, xform: xform);
        _transform.AttachToGridOrMap(uid, xform);

        // Reset whether the projectile has damaged anything if it successfully was removed
        if (TryComp<ProjectileComponent>(uid, out var projectile))
        {
            projectile.Shooter = null;
            projectile.Weapon = null;
            projectile.DamagedEntity = false;
        }

        FreePenetrated(uid);

        // Land it just coz uhhh yeah
        var landEv = new LandEvent(args.User, true);
        RaiseLocalEvent(uid, ref landEv);
        _physics.WakeBody(uid, body: physics);

        // try place it in the user's hand
        _hands.TryPickupAnyHand(args.User, uid);
    }

    private bool AttemptEmbedRemove(EntityUid uid, EntityUid user, EmbeddableProjectileComponent? component = null)
    {
        if (!Resolve(uid, ref component, false)
            || component.RemovalTime == null
            || !TryComp<PhysicsComponent>(uid, out var physics)
            || physics.BodyType != BodyType.Static)
            return false;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, component.RemovalTime.Value,
            new RemoveEmbeddedProjectileEvent(), eventTarget: uid, target: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
        });

        return true;
    }

    private void FreePenetrated(EntityUid uid, PenetratedProjectileComponent? penetratedProjectile = null)
    {
        if (!Resolve(uid, ref penetratedProjectile)
            || !penetratedProjectile.PenetratedUid.HasValue)
            return;

        _penetrated.FreePenetrated(penetratedProjectile.PenetratedUid.Value);
    }
    // WD EDIT END
}
