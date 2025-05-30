using System.Numerics;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Item;
using Content.Shared.Mobs.Systems;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server._White.Throwing;

public sealed class ReturnItemOnThrowSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    // How close should the item be to complete its movement
    private const float FinishMovementDistance = 0.2f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReturnItemOnThrowComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<ReturnItemOnThrowComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
    }

    private void OnThrowHit(EntityUid uid, ReturnItemOnThrowComponent component, ThrowDoHitEvent args)
    {
        var thrower = args.Component.Thrower;
        if (!thrower.HasValue
            || component.ThrowerWhitelist is { } throwerWhitelist
                && !_entityWhitelist.IsValid(throwerWhitelist, thrower)
            || component.TargetWhitelist is { } targetWhitelist
                && !_entityWhitelist.IsValid(targetWhitelist, args.Target)
            || component.TargetBlacklist is { } targetBlacklist
                && _entityWhitelist.IsValid(targetBlacklist, args.Target)
            || _mobStateSystem.IsDead(thrower.Value))
            return;

        StartReturnAnimation(uid, thrower.Value, component);
    }

    private void OnPickupAttempt(EntityUid uid, ReturnItemOnThrowComponent component, GettingPickedUpAttemptEvent args)
    {
        if (component.ReturningTo.HasValue)
            args.Cancel();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ReturnItemOnThrowComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ReturningTo == null || !Exists(component.ReturningTo.Value))
            {
                component.ReturningTo = null;
                continue;
            }

            // Get current item position and target position
            var currentPos = _transform.GetWorldPosition(uid);
            var targetPos = _transform.GetWorldPosition(component.ReturningTo.Value);

            // Calculate direction and distance
            var direction = targetPos - currentPos;
            var distance = direction.Length();

            // If item is close enough to target, finish movement
            if (distance < FinishMovementDistance)
            {
                FinishShieldReturn(uid, component);
                continue;
            }

            // Normalize direction and calculate new position
            direction = Vector2.Normalize(direction);
            var movement = direction * component.ReturnSpeed * frameTime;

            // If item would move further than target in one frame, place it directly in hands
            if (movement.Length() >= distance)
            {
                FinishShieldReturn(uid, component);
                continue;
            }

            // Move item to new position
            var newPos = currentPos + movement;
            _transform.SetWorldPosition(uid, newPos);

            // Rotate item in direction of movement
            var angle = direction.ToWorldAngle();
            _transform.SetWorldRotation(uid, angle);
        }
    }

    /// <summary>
    /// Starts animation of shield return directly to owner
    /// </summary>
    private void StartReturnAnimation(EntityUid uid, EntityUid thrower, ReturnItemOnThrowComponent component)
    {
        component.ReturningTo = thrower;
        if (TryComp<PhysicsComponent>(uid, out var physicsComponent))
            _physics.SetCanCollide(uid, false, body: physicsComponent);
    }

    /// <summary>
    /// Completes shield return by placing it in owner's hands
    /// </summary>
    private void FinishShieldReturn(EntityUid uid, ReturnItemOnThrowComponent component)
    {
        if (component.ReturningTo == null || !Exists(component.ReturningTo.Value))
        {
            component.ReturningTo = null;
            return;
        }

        if (TryComp<PhysicsComponent>(uid, out var physicsComponent))
            _physics.SetCanCollide(uid, true, body: physicsComponent);

        var returningTo = component.ReturningTo.Value;
        component.ReturningTo = null;

        var message = Loc.GetString(
            "return-item-to-hands",
            ("item", Identity.Entity(uid, EntityManager)));
        var messageToOther = Loc.GetString(
            "return-item-to-hands-other",
            ("item", Identity.Entity(uid, EntityManager)),
            ("user", Identity.Entity(returningTo, EntityManager)));

        if (!_hands.TryPickupAnyHand(returningTo, uid))
        {
            message = Loc.GetString(
                "return-item-to-feet",
                ("item", Identity.Entity(uid, EntityManager)));
            messageToOther = Loc.GetString(
                "return-item-to-feet-other",
                ("item", Identity.Entity(uid, EntityManager)),
                ("user", Identity.Entity(returningTo, EntityManager)));
            _transform.SetWorldPosition(uid,  _transform.GetWorldPosition(returningTo));
        }

        _popup.PopupEntity(message, uid, returningTo);
        _popup.PopupEntity(messageToOther, uid, Filter.PvsExcept(returningTo, entityManager: EntityManager), true);
    }
}
