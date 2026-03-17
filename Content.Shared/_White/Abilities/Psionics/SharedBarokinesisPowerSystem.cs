using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;

namespace Content.Shared._White.Abilities.Psionics;

public abstract partial class SharedBarokinesisPowerSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public void Push(EntityUid target, EntityUid user, float force, float throwSpeed, PhysicsComponent targetPhysics)
    {
        var userPos = _transform.GetWorldPosition(user);
        var targetPos = _transform.GetWorldPosition(target);

        var direction = targetPos - userPos;

        var impulse = direction.Normalized() * force * targetPhysics.Mass;

        _throwing.TryThrow(target, impulse, throwSpeed);

    }

    public void Pull(EntityUid target, EntityUid user, float force, float throwSpeed, PhysicsComponent targetPhysics)
    {
        var userPos = _transform.GetWorldPosition(user);
        var targetPos = _transform.GetWorldPosition(target);

        var direction = userPos - targetPos;

        var impulse = direction.Normalized() * force * targetPhysics.Mass;

        _throwing.TryThrow(target, impulse, throwSpeed);
    }

    public void Dash(EntityUid user, float force, float throwSpeed)
    {
        var xform = Transform(user);
        var direction = xform.LocalRotation.ToWorldVec() * force;
        var impulse = xform.Coordinates.Offset(direction);

        _throwing.TryThrow(user, impulse, throwSpeed / 2);
    }
}
