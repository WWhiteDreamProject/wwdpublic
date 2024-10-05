using System.Numerics;
using Content.Shared._White.Penetrated;
using Content.Shared._White.Projectile;
using Content.Shared.Buckle;
using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._White.Projectiles;

public sealed class PenetratedProjectileSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly PenetratedSystem _penetrated = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PenetratedProjectileComponent, AttemptEmbedEvent>(OnEmbed);
        SubscribeLocalEvent<PenetratedProjectileComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<PenetratedProjectileComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<PenetratedProjectileComponent, EntityTerminatingEvent>(OnEntityTerminating);
    }

    private void OnEmbed(EntityUid uid, PenetratedProjectileComponent component, AttemptEmbedEvent args)
    {
        if (!TryComp(args.Embedded, out PenetratedComponent? penetrated)
            || penetrated.IsPinned
            || component.PenetratedUid.HasValue
            || !TryComp(uid, out PhysicsComponent? physics)
            || !TryComp(args.Embedded, out PhysicsComponent? physicEmbedded)
            || physics.LinearVelocity.Length() < component.MinimumSpeed)
            return;

        component.PenetratedUid = args.Embedded;
        penetrated.ProjectileUid = uid;
        penetrated.IsPinned = true;

        _buckle.TryUnbuckle(args.Embedded, args.Embedded, true);
        _damageable.TryChangeDamage(args.Embedded, component.Damage, origin: args.Shooter);
        _physics.SetLinearVelocity(args.Embedded, Vector2.Zero, body: physicEmbedded);
        _physics.SetBodyType(args.Embedded, BodyType.Static, body: physicEmbedded);
        var xform = Transform(args.Embedded);
        _transform.AttachToGridOrMap(args.Embedded, xform);
        _transform.SetLocalPosition(args.Embedded, Transform(uid).LocalPosition, xform);
        _transform.SetParent(args.Embedded, xform, uid);
        _physics.SetLinearVelocity(uid, physics.LinearVelocity / 2, body: physics);
    }

    private void OnLand(EntityUid uid, PenetratedProjectileComponent component, ref LandEvent args)
    {
        FreePenetrated(uid, component);
    }

    private void OnEntityTerminating(EntityUid uid, PenetratedProjectileComponent component, ref EntityTerminatingEvent args)
    {
        FreePenetrated(uid, component);
    }

    private void OnRemove(EntityUid uid, PenetratedProjectileComponent component, ComponentRemove args)
    {
        FreePenetrated(uid, component);
    }

    private void FreePenetrated(EntityUid uid, PenetratedProjectileComponent component)
    {
        if (!component.PenetratedUid.HasValue)
            return;

        var penetratedUid = component.PenetratedUid.Value;
        _penetrated.FreePenetrated(penetratedUid);

        if (TryComp<EmbeddableProjectileComponent>(uid, out var embeddable))
            _projectile.Embed(uid, penetratedUid, null, embeddable, false);
    }
}
