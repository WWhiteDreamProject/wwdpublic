using Content.Shared._White.PAI.Components;
using Content.Shared._White.PAI.Events;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using System.Numerics;

namespace Content.Shared._White.PAI.Systems;

public sealed class ManipulatorSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ManipulatorComponent, ManipulatorToggleActionEvent>(OnToggle);
        SubscribeLocalEvent<ManipulatorComponent, ComponentShutdown>(OnShutdown);
        SubscribeNetworkEvent<ManipulatorMoveEvent>(OnMove);
        SubscribeNetworkEvent<ManipulatorGrabEvent>(OnGrab);
        SubscribeNetworkEvent<ManipulatorInteractEvent>(OnInteract);
        SubscribeLocalEvent<UsedByManipulatorComponent, EntParentChangedMessage>(OnParentChanged);
    }

    private void OnToggle(EntityUid uid, ManipulatorComponent comp, ManipulatorToggleActionEvent args)
    {
        if (comp.IsActive)
        {
            Detach(comp, uid);

            comp.IsActive = false;
            comp.IsReturning = true;
            comp.TargetWorldPos = _transform.GetMapCoordinates(uid);
            args.Handled = true;
        }
        if (comp.IsReturning)
            return;

        if (_netMan.IsServer)
        {
            comp.Manipulator = SpawnAtPosition(comp.ManipulatorProto, Transform(uid).Coordinates);
            var man = comp.Manipulator;
            var visuals = EnsureComp<JointVisualsComponent>(man.Value);
            visuals.Sprite = comp.JointSpite;
            visuals.OffsetA = new Vector2(0f, 0f);
            visuals.Target = GetNetEntity(uid);
            Dirty(man.Value, visuals);
        }

        comp.IsActive = true;
        Dirty(uid, comp);
        args.Handled = true;
    }

    private void OnGrab(ManipulatorGrabEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ent
            || !TryComp<ManipulatorComponent>(ent, out var man))
            return;

        if (!man.IsActive || man.Manipulator == null || man.IsReturning)
            return;

        if (man.IsGrabbing)
        {
            Detach(man, ent);
            return;
        }

        var target = GetEntity(msg.Ent);

        if (target == ent || target == man.Manipulator)
            return;

        if (!EntityManager.EntityExists(target))
            return;

        if (!HasComp<ItemComponent>(target))
            return;

        if (!TryComp<PhysicsComponent>(target, out var phys))
            return;

        if (phys.BodyStatus != BodyStatus.OnGround)
            return;

        if (_container.IsEntityInContainer(target))
            return;

        _transform.SetParent(target, man.Manipulator.Value);

        man.GrabbedEntity = target;
        var marker = EnsureComp<UsedByManipulatorComponent>(target);
        marker.ManipulatorOwner = ent;

        man.IsGrabbing = true;
    }

    private void OnMove(ManipulatorMoveEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ent
            || !TryComp<ManipulatorComponent>(ent, out var man))
            return;

        if (!man.IsActive || man.Manipulator == null || man.IsReturning)
            return;

        if (msg.Coords.MapId != Transform(ent).MapID)
            return;

        man.TargetWorldPos = msg.Coords;
    }

    private void OnInteract(ManipulatorInteractEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ent
            || !TryComp<ManipulatorComponent>(ent, out var man))
            return;

        if (man.GrabbedEntity == null)
            return;

        _interact.UseInHandInteraction(ent, man.GrabbedEntity.Value, false, false, true);
    }

    private void Detach(ManipulatorComponent comp, EntityUid uid)
    {
        if (comp.Manipulator == null)
            return;

        if (comp.GrabbedEntity != null && EntityManager.EntityExists(comp.GrabbedEntity))
        {
            var ent = comp.GrabbedEntity.Value;
            var manipulatorCoords = _transform.GetMoverCoordinates(comp.Manipulator.Value);

            _transform.AttachToGridOrMap(ent, Transform(ent));
            _transform.SetCoordinates(ent, manipulatorCoords);

            RemComp<UsedByManipulatorComponent>(ent);
        }

        comp.GrabbedEntity = null;
        Dirty(uid, comp);
    }

    private void OnParentChanged(EntityUid uid, UsedByManipulatorComponent comp, ref EntParentChangedMessage args)
    {
        if (TryComp<ManipulatorComponent>(comp.ManipulatorOwner, out var man))
        {
            if (args.Transform.ParentUid == man.Manipulator)
                return;

            man.GrabbedEntity = null;
            man.IsGrabbing = false;
        }

        RemComp<UsedByManipulatorComponent>(uid);
    }

    private void OnShutdown(EntityUid uid, ManipulatorComponent comp, ComponentShutdown args)
    {
        Detach(comp, uid);
        QueueDel(comp.Manipulator);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ManipulatorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if ((!comp.IsActive && !comp.IsReturning) || comp.Manipulator == null)
                continue;

            var man = comp.Manipulator.Value;

            if (!TryComp<PhysicsComponent>(man, out var physics))
                continue;

            if (comp.IsReturning)
            {
                comp.TargetWorldPos = _transform.GetMapCoordinates(uid);
            }

            if (comp.TargetWorldPos == null)
                continue;

            var curPos = _transform.GetWorldPosition(man);
            var targetPos = comp.TargetWorldPos.Value.Position;

            var toTarget = targetPos - curPos;
            var distance = toTarget.Length();

            if (distance < 0.1f)
            {
                _physics.SetLinearVelocity(man, Vector2.Zero, body: physics);
                comp.TargetWorldPos = null;

                if (comp.IsReturning)
                {
                    QueueDel(man);
                    comp.Manipulator = null;
                    comp.IsReturning = false;
                }
                continue;
            }

            var direction = toTarget.Normalized();
            var velocity = direction * comp.ManipulatorSpeed;

            _physics.SetLinearVelocity(man, velocity, body: physics);
        }
    }
}

