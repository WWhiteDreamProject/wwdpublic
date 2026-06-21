using Content.Shared._White.Psionics.Abilities;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared._White.Abilities.Psionics;

public abstract partial class SharedBarokinesisPowerSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public void Push(EntityUid target, EntityUid user, float force, float throwSpeed, PhysicsComponent targetPhysics, BarokinesisPowerComponent component)
    {
        var userPos = _transform.GetWorldPosition(user);
        var targetPos = _transform.GetWorldPosition(target);

        var weakness = WeaknessThing(component);

        var direction = targetPos - userPos;
        var impulse = direction.Normalized() * force * weakness;

        _throwing.TryThrow(target, impulse, throwSpeed);
        component.LastUsedTime = _timing.CurTime;
    }

    public void Pull(EntityUid target, EntityUid user, float force, float throwSpeed, PhysicsComponent targetPhysics, BarokinesisPowerComponent component)
    {
        var userPos = _transform.GetWorldPosition(user);
        var targetPos = _transform.GetWorldPosition(target);

        var weakness = WeaknessThing(component);

        var direction = userPos - targetPos;
        var impulse = direction.Normalized() * force * weakness;

        _throwing.TryThrow(target, impulse, throwSpeed);
        component.LastUsedTime = _timing.CurTime;
    }

    public void Dash(EntityUid user, float force, float throwSpeed, BarokinesisPowerComponent component)
    {
        var xform = Transform(user);
        var direction = xform.LocalRotation.ToWorldVec() * force;

        var weakness = WeaknessThing(component);

        var impulse = xform.Coordinates.Offset(direction) * weakness;

        _throwing.TryThrow(user, impulse, throwSpeed / 2);
        component.LastUsedTime = _timing.CurTime;
    }

    private float WeaknessThing(BarokinesisPowerComponent component)
    {
        var lastUsedTime = (_timing.CurTime - component.LastUsedTime).TotalSeconds;
        var recovery = (float) Math.Clamp(
            (lastUsedTime - component.MinimumRecovery) /
            (component.MaximumRecovery - component.MinimumRecovery), 0f, 1f);

        var weakness = MathHelper.Lerp(component.MinimumWeakness, component.MaximumWeakness, recovery);

        return weakness;
    }
}
