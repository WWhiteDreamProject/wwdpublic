using Content.Shared._White.PAI.Components;
using Content.Shared._White.PAI.Events;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using System.Numerics;

namespace Content.Server._White.PAI.Systems;

public sealed class ManipulatorSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ManipulatorComponent, ManipulatorToggleActionEvent>(OnToggle);
        SubscribeLocalEvent<ManipulatorComponent, ManipulatorGrabToggleActionEvent>(OnGrab);
        SubscribeLocalEvent<ManipulatorComponent, ManipulatorMoveActionEvent>(OnMove);
        SubscribeLocalEvent<ManipulatorComponent, ManipulatorInteractActionEvent>(OnInteract);
        SubscribeLocalEvent<UsedByManipulatorComponent, EntParentChangedMessage>(OnParentChanged);
    }

    private void OnToggle(EntityUid uid, ManipulatorComponent comp, ManipulatorToggleActionEvent args)
    {
        if (comp.IsActive)
        {
            Detach(comp);

            comp.IsActive = false;
            comp.IsReturning = true;
            comp.TargetWorldPos = _transform.GetMapCoordinates(uid);
            args.Handled = true;
        }
        else
        {
            if (comp.IsReturning)
                return;

            comp.Manipulator = SpawnAtPosition(comp.ManipulatorProto, Transform(uid).Coordinates);
            var man = comp.Manipulator;
            var visuals = EnsureComp<JointVisualsComponent>(man.Value);

            visuals.Sprite = comp.JointSpite;
            visuals.OffsetA = new Vector2(0f, 0f);
            visuals.Target = GetNetEntity(uid);
            Dirty(man.Value, visuals);

            comp.IsActive = true;
            args.Handled = true;
        }
    }

    private void OnGrab(EntityUid uid, ManipulatorComponent comp, ManipulatorGrabToggleActionEvent args)
    {
        if (!comp.IsActive)
            return;

        if (comp.Manipulator == null)
            return;

        if (comp.IsGrabbin)
        {
            Detach(comp);
            args.Handled = true;
            return;
        }

        var coords = _transform.GetMapCoordinates(comp.Manipulator.Value);

        var entitiesUnderneath = _lookup.GetEntitiesInRange(coords, 0.1f);

        foreach (var entity in entitiesUnderneath)
        {
            if (entity == uid || entity == comp.Manipulator)
                continue;

            if (!EntityManager.EntityExists(entity))
                return;

            if (!HasComp<ItemComponent>(entity))
                continue;

            if (!TryComp<PhysicsComponent>(entity, out var phys))
                continue;

            if (phys.BodyStatus != BodyStatus.OnGround)
                continue;

            if (_container.IsEntityInContainer(entity))
                continue;

            comp.GrabbedEntity = entity;
            _transform.SetParent(entity, comp.Manipulator.Value);

            var marker = EnsureComp<UsedByManipulatorComponent>(entity);
            marker.ManipulatorOwner = uid;

            comp.IsGrabbin = true;
            break;
        }
        args.Handled = true;
    }

    private void OnMove(EntityUid uid, ManipulatorComponent comp, ManipulatorMoveActionEvent args)
    {
        if (!comp.IsActive || comp.Manipulator == null || comp.IsReturning)
            return;

        comp.TargetWorldPos = _transform.ToMapCoordinates(args.Target);
        args.Handled = true;
    }

    private void OnInteract(EntityUid uid, ManipulatorComponent comp, ManipulatorInteractActionEvent args)
    {
        var ent = comp.GrabbedEntity;

        if (ent == null)
            return;

        _interact.UseInHandInteraction(uid, ent.Value, false, false, true);

    }
    private void Detach(ManipulatorComponent comp)
    {
        if (comp.Manipulator == null)
            return;

        if (comp.GrabbedEntity != null && EntityManager.EntityExists(comp.GrabbedEntity.Value))
        {
            var manipulatorCoords = _transform.GetMoverCoordinates(comp.Manipulator.Value);

            _transform.AttachToGridOrMap(comp.GrabbedEntity.Value, Transform(comp.GrabbedEntity.Value));
            _transform.SetCoordinates(comp.GrabbedEntity.Value, manipulatorCoords);

            RemComp<UsedByManipulatorComponent>(comp.GrabbedEntity.Value);
        }
        else
        {
            comp.GrabbedEntity = null;
        }

        comp.IsGrabbin = false;
    }

    private void OnParentChanged(EntityUid uid, UsedByManipulatorComponent comp, ref EntParentChangedMessage args)
    {
        if (TryComp<ManipulatorComponent>(comp.ManipulatorOwner, out var man))
        {
            if (args.Transform.ParentUid == man.Manipulator)
                return;

            man.GrabbedEntity = null;
            man.IsGrabbin = false;
        }

        RemComp<UsedByManipulatorComponent>(uid);
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
