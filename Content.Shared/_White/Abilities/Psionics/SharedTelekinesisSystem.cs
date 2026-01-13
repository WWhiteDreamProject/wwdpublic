using Content.Shared._White.Psionics.Abilities;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Abilities.Psionics;

public abstract partial class SharedTelekinesisPowerSystem : EntitySystem
{
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<RequestTelekinesisMoveEvent>(OnTelekinesisMove);
    }

    protected virtual void StartTether(EntityUid userUid, TelekinesisPowerComponent component, EntityUid target,
        PhysicsComponent? targetPhysics = null, TransformComponent? targetXform = null)
    {
        if (!Resolve(target, ref targetPhysics, ref targetXform))
            return;
        if (!TryComp<PsionicComponent>(userUid, out var psionic))
            return;

        if (targetPhysics.Mass > component.MaxMass * psionic.CurrentAmplification)
        {
            _popup.PopupCursor(Loc.GetString("telekinesis-big-mass"), userUid);
            return;
        }

        if (component.TetheredEntity != null)
            StopTether(userUid, component);

        var amplification = psionic.CurrentAmplification;
        var dampening = psionic.CurrentDampening;

        component.TetheredEntity = target;

        var tethered = EnsureComp<TelekinesisTargetComponent>(target);
        _physics.SetBodyStatus(target, targetPhysics, BodyStatus.InAir, false);
        _physics.SetSleepingAllowed(target, targetPhysics, false);
        tethered.Tetherer = userUid;
        tethered.OriginalAngularDamping = targetPhysics.AngularDamping;
        _physics.SetAngularDamping(target, targetPhysics, 0f);
        tethered.OriginalLinearDamping = targetPhysics.LinearDamping;
        _physics.SetLinearDamping(target, targetPhysics, 2f);
        _physics.WakeBody(target, body: targetPhysics);

        var tether = Spawn("TetherEntity", _transform.GetMapCoordinates(target));
        var tetherPhysics = Comp<PhysicsComponent>(tether);
        component.TetherPoint = tether;
        _physics.WakeBody(tether);

        float frequency = component.Frequency * dampening;
        float dampingRatio = MathHelper.Lerp(0.6f, 1.0f, Math.Clamp(dampening, 0, 1.5f) / 1.5f);

        string jointId = $"pull-joint-telekinesis-{GetNetEntity(userUid)}-{GetNetEntity(target)}";
        var joint = _joints.CreateMouseJoint(tether, target, id: jointId);
        component.JointId = jointId;

        SharedJointSystem.LinearStiffness(frequency, dampingRatio,
            tetherPhysics.Mass, targetPhysics.Mass, out var stiffness, out var damping);

        joint.Stiffness = stiffness;
        joint.Damping = damping;
        joint.MaxForce = component.MaxForce * amplification;

        Dirty(target, tethered);
        Dirty(userUid, component);
    }

    protected virtual void StopTether(EntityUid uid, TelekinesisPowerComponent component)
    {
        if (component.TetheredEntity == null)
            return;

        var targetValue = component.TetheredEntity.Value;

        if (!TryComp<TelekinesisTargetComponent>(targetValue, out var comp))
            return;

        if (component.JointId != null && component.TetherPoint != null)
        {
            _joints.RemoveJoint(targetValue, component.JointId);
            component.JointId = null;
            QueueDel(component.TetherPoint);
        }

        if (TryComp<PhysicsComponent>(component.TetheredEntity, out var targetPhysics))
        {
            _physics.SetBodyStatus(targetValue, targetPhysics, BodyStatus.OnGround);
            _physics.SetSleepingAllowed(targetValue, targetPhysics, true);
            _physics.SetAngularDamping(targetValue, targetPhysics, comp.OriginalAngularDamping);
            _physics.SetLinearDamping(targetValue, targetPhysics, comp.OriginalLinearDamping);
        }

        RemComp<TelekinesisTargetComponent>(targetValue);
        component.TetheredEntity = null;
        Dirty(uid, component);
    }

    private void OnTelekinesisMove(RequestTelekinesisMoveEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        if (!TryComp<TelekinesisPowerComponent>(user, out var power))
            return;

        if (power.TetherPoint == null || power.TetheredEntity == null)
            return;

        var coords = GetCoordinates(msg.Coordinates);

        var userCoords = _transform.GetMapCoordinates(user.Value);
        var targetCoords = _transform.ToMapCoordinates(coords);

        const float maxDistance = 15f;
        if ((userCoords.Position - targetCoords.Position).Length() > maxDistance)
            return;

        _transform.SetCoordinates(power.TetherPoint.Value, coords);
    }

    [Serializable, NetSerializable]
    protected sealed class RequestTelekinesisMoveEvent : EntityEventArgs
    {
        public NetCoordinates Coordinates;
    }
}
