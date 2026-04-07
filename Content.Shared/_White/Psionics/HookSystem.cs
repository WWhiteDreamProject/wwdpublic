using Content.Shared._White.Psionics.Abilities;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Misc;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared._White.Psionics;

public sealed class HookSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedLayingDownSystem _layingDown = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PsionicHookComponent, GunShotEvent>(OnShot);
        SubscribeLocalEvent<ProjectilePsionicHookComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnShot(EntityUid uid, PsionicHookComponent component, ref GunShotEvent args)
    {
        foreach (var (shotUid, _) in args.Ammo)
        {
            if (!TryComp<ProjectilePsionicHookComponent>(shotUid, out var hookshot))
                continue;

            component.Projectile = shotUid.Value;
            hookshot.Gun = uid;

            Dirty(uid, component);

            var visuals = EnsureComp<JointVisualsComponent>(shotUid.Value);
            visuals.Sprite = component.RopeSprite;
            visuals.OffsetA = new Vector2(0f, 0.5f);
            visuals.Target = GetNetEntity(uid);
            Dirty(shotUid.Value, visuals);
        }

        _appearance.SetData(uid, SharedTetherGunSystem.TetherVisualsStatus.Key, false);
        Dirty(uid, component);
    }

    private void OnProjectileHit(Entity<ProjectilePsionicHookComponent> ent, ref ProjectileHitEvent args)
    {
        if (!Timing.IsFirstTimePredicted ||
        !TryComp<ProjectileComponent>(ent, out var projectile) ||
        projectile.Shooter == null)
            return;

        if (!TryComp<PsionicComponent>(projectile.Shooter, out var psionic) ||
            !TryComp<PhysicsComponent>(ent, out var phys))
            return;

        HookThrow(ent, args, projectile, psionic);

        _physics.SetBodyStatus(ent, phys, BodyStatus.OnGround);
        _physics.SetLinearVelocity(ent, Vector2.Zero);

    }

    private void HookThrow(Entity<ProjectilePsionicHookComponent> ent, ProjectileHitEvent args,
        ProjectileComponent projectile, PsionicComponent psionic)
    {
        var power = MathF.Pow(psionic.CurrentAmplification, 1.3f);
        var hookComp = ent.Comp;

        if (projectile.Shooter is null)
            return;

        var shooter = projectile.Shooter.Value;

        var shooterPos = _transform.GetWorldPosition(shooter);
        var targetPos = _transform.GetWorldPosition(args.Target);

        if (HasComp<MobStateComponent>(args.Target)) // if we shot mob
        {
            var direction = (shooterPos - targetPos).Normalized();
            var force = direction * power;
            _layingDown.TryLieDown(args.Target);
            _throw.TryThrow(args.Target, force, hookComp.BasePower, shooter);
            StartReturnToGun(ent, ent);
        }
        else if (_tags.HasTag(args.Target, "Wall")) // if we shot wall
        {
            var direction = (targetPos - shooterPos).Normalized();
            var force = direction * power;
            _throw.TryThrow(shooter, force, hookComp.BasePower, shooter);
            StartReturnToGun(ent, ent);
        }
    }

    private void StartReturnToGun(EntityUid projectile, ProjectilePsionicHookComponent component)
    {
        component.IsReturning = true;

        Dirty(projectile, component);

        if (TryComp<JointVisualsComponent>(projectile, out var visuals))
            Dirty(projectile, visuals);
    }

    private void GimmeHookBack(EntityUid uid, ProjectilePsionicHookComponent component)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        if (Deleted(uid))
            return;

        var gun = component.Gun;
        if (TryComp<PsionicHookComponent>(gun, out var hookComp))
        {
            hookComp.Projectile = null;
            Dirty(gun, hookComp);
        }

        _gun.ChangeBasicEntityAmmoCount(gun, 1);

        _appearance.SetData(uid, SharedTetherGunSystem.TetherVisualsStatus.Key, true);

        QueueDel(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ProjectilePsionicHookComponent>();

        while (query.MoveNext(out var uid, out var hookComp))
        {
            if (!hookComp.IsReturning)
                continue;

            if (Deleted(hookComp.Gun))
            {
                QueueDel(uid);
                continue;
            }

            var sourcePos = _transform.GetWorldPosition(hookComp.Gun);
            var projectilePos = _transform.GetWorldPosition(uid);

            var direction = sourcePos - projectilePos;
            var distance = direction.Length();

            if (distance < 0.3f)
            {
                if (_netManager.IsServer)
                    GimmeHookBack(uid, hookComp);

                continue;
            }

            var movement = direction.Normalized() * hookComp.ReturnSpeed * frameTime;
            _transform.SetWorldPosition(uid, projectilePos + movement);

            if (TryComp<JointVisualsComponent>(uid, out var visuals))
            {
                visuals.OffsetA = Vector2.Zero;
                Dirty(uid, visuals);
            }
        }
    }
}
