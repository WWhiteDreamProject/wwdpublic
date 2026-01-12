using Content.Shared._White.Actions.Events;
using Content.Shared._White.Psionics.Abilities;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Throwing;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Misc;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using static Content.Shared.Weapons.Misc.SharedTetherGunSystem;

namespace Content.Shared._White.Abilities.Psionics;

public abstract partial class SharedTelekinesisPowerSystem : EntitySystem
{
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrownItemSystem _thrown = default!;

    private const string TetherJoint = "telekinesis";
    private const float SpinVelocity = MathF.PI;

    public override void Initialize()
    {
        base.Initialize();
        //SubscribeAllEvent<RequestTelekinesisMoveEvent>(OnTelekinesisMove);
    }

    protected virtual void StartTether(EntityUid userUid, TelekinesisPowerComponent component, EntityUid target, EntityUid user,
        PhysicsComponent? targetPhysics = null, TransformComponent? targetXform = null)
    {
        if (!Resolve(target, ref targetPhysics, ref targetXform))
            return;
        if (!TryComp<PsionicComponent>(userUid, out var psionic))
            return;

        var amplification = psionic.CurrentAmplification;
        var dampening = psionic.CurrentDampening;

        _transform.Unanchor(target, targetXform);

        component.TetheredEntity = target;

        var tethered = EnsureComp<TelekinesisTargetComponent>(target);
        _physics.SetBodyStatus(target, targetPhysics, BodyStatus.InAir, false);
        _physics.SetSleepingAllowed(target, targetPhysics, false);
        tethered.Tetherer = userUid;
        tethered.OriginalAngularDamping = targetPhysics.AngularDamping;
        _physics.SetAngularDamping(target, targetPhysics, 0f);
        _physics.SetLinearDamping(target, targetPhysics, 0f);
        _physics.SetAngularVelocity(target, SpinVelocity, body: targetPhysics);
        _physics.WakeBody(target, body: targetPhysics);

        var tether = Spawn("TetherEntity", _transform.GetMapCoordinates(target));
        var tetherPhysics = Comp<PhysicsComponent>(tether);
        component.TetherPoint = tether;
        _physics.WakeBody(tether);

        float frequency = component.Frequency * dampening;
        float dampingRatio = MathHelper.Lerp(0.6f, 1.0f, Math.Clamp(dampening, 0, 1.5f) / 1.5f);

        var joint = _joints.CreateMouseJoint(tether, target, id: TetherJoint);
        SharedJointSystem.LinearStiffness(frequency, dampingRatio,
            tetherPhysics.Mass, targetPhysics.Mass, out var stiffness, out var damping);

        joint.Stiffness = stiffness;
        joint.Damping = damping;
        joint.MaxForce = component.MaxForce * amplification;

        var powerEv = new TelekinesisPowerActionEvent { Target = target };
        RaiseLocalEvent(userUid, powerEv);

        Dirty(target, tethered);
        Dirty(userUid, component);
    }

    protected virtual void StopTether(EntityUid uid, TelekinesisPowerComponent component, bool land = true, bool transfer = false)
    {
        if (component.TetheredEntity == null)
            return;

        var targetValue = component.TetheredEntity.Value;

        if (component.TetheredEntity != null && component.TetherPoint != null)
        {
            _joints.RemoveJoint(targetValue, TetherJoint);
            QueueDel(component.TetherPoint.Value);
            component.TetheredEntity = null;
        }

        if (TryComp<PhysicsComponent>(component.TetheredEntity, out var targetPhysics))
        {
            if (land)
            {
                var thrown = EnsureComp<ThrownItemComponent>(targetValue);
                _thrown.LandComponent(targetValue, thrown, targetPhysics, true);
                _thrown.StopThrow(targetValue, thrown);
            }

            _physics.SetBodyStatus(targetValue, targetPhysics, BodyStatus.OnGround);
            _physics.SetSleepingAllowed(targetValue, targetPhysics, true);
            _physics.SetAngularDamping(targetValue, targetPhysics, Comp<TelekinesisTargetComponent>(targetValue).OriginalAngularDamping);
        }

        RemComp<TelekinesisTargetComponent>(targetValue);
        component.TetheredEntity = null;
        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TelekinesisPowerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.TetherPoint == null || comp.TetheredEntity == null)
                continue;

            if (!TryComp<TelekinesisPowerComponent>(uid, out var userComp))
                continue;

            var worldPos = _transform.ToMapCoordinates(comp.TargetCoordinates);

            // Двигаем точку привязки
            _transform.SetWorldPosition(comp.TetherPoint.Value, worldPos.Position);
        }
    }

    [Serializable, NetSerializable]
    protected sealed class RequestTelekinesisMoveEvent : EntityEventArgs
    {
        public NetCoordinates Coordinates;
    }
}
