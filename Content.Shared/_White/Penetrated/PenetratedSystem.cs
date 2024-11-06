using Content.Shared._White.Projectile;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._White.Penetrated;

public sealed class PenetratedSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public void FreePenetrated(EntityUid uid, PenetratedComponent? penetrated = null, PhysicsComponent? physics = null)
    {
        var xform = Transform(uid);
        _transform.AttachToGridOrMap(uid, xform);

        if (Resolve(uid, ref physics, false))
        {
            _physics.SetBodyType(uid, BodyType.KinematicController, body: physics, xform: xform);
            _physics.WakeBody(uid, body: physics);
        }

        if (!Resolve(uid, ref penetrated, false))
            return;

        penetrated.IsPinned = false;

        if (TryComp<PenetratedProjectileComponent>(penetrated.ProjectileUid, out var penetratedProjectile))
            penetratedProjectile.PenetratedUid = null;

        penetrated.ProjectileUid = null;
    }
}
