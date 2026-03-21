using Content.Shared._White.Psionics.Abilities;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;

namespace Content.Shared._White.Abilities.Psionics;

public abstract partial class SharedBarokinesisPowerSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public void Push(EntityUid target, EntityUid user, float force, float throwSpeed, PhysicsComponent targetPhysics, BarokinesisPowerComponent component)
    {
        var userPos = _transform.GetWorldPosition(user);
        var targetPos = _transform.GetWorldPosition(target);

        var direction = targetPos - userPos;
        var impulse = direction.Normalized() * force * component.CurrentWeakness * targetPhysics.Mass;

        component.CurrentWeakness = Math.Max(component.MinimumWeakness, component.CurrentWeakness * component.DecayWeakness);

        _throwing.TryThrow(target, impulse, throwSpeed);

    }

    public void Pull(EntityUid target, EntityUid user, float force, float throwSpeed, PhysicsComponent targetPhysics, BarokinesisPowerComponent component)
    {
        var userPos = _transform.GetWorldPosition(user);
        var targetPos = _transform.GetWorldPosition(target);

        var direction = userPos - targetPos;
        var impulse = direction.Normalized() * force * component.CurrentWeakness * targetPhysics.Mass;

        component.CurrentWeakness = Math.Max(component.MinimumWeakness, component.CurrentWeakness * component.DecayWeakness);

        _throwing.TryThrow(target, impulse, throwSpeed);
    }

    public void Dash(EntityUid user, float force, float throwSpeed, BarokinesisPowerComponent component)
    {
        var xform = Transform(user);
        var direction = xform.LocalRotation.ToWorldVec() * force;
        var impulse = xform.Coordinates.Offset(direction) * component.CurrentWeakness;

        component.CurrentWeakness = Math.Max(component.MinimumWeakness, component.CurrentWeakness * component.DecayWeakness);

        _throwing.TryThrow(user, impulse, throwSpeed / 2);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BarokinesisPowerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CurrentWeakness < comp.MaximumWeakness)
            {
                comp.CurrentWeakness = Math.Min(comp.MaximumWeakness, comp.CurrentWeakness + comp.RecoveryWeakness * frameTime);
            }
        }
    }
}
