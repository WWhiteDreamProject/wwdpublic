using Content.Server.Stunnable.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Throwing;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Items;
using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Stunnable;
using Content.Shared.Mobs.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Hands.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using System.Linq;
using Robust.Shared.Physics.Components;
using Content.Shared.Item;
using System.Numerics;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;

namespace Content.Server.WhiteDream.BloodCult.Items;

/// <summary>
/// The system responsible for returning items with a ReturnableThrowingComponent to their owner and the handling of picking attempts by non-cultists.
/// </summary>
public sealed class ReturnableThrowingSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrownItemSystem _thrownSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    private const string MessageReturnedToHands = "returnable-cult-item-returned-to-hands";
    private const string MessageReturnedToFeet = "returnable-cult-item-returned-to-feet";
    
    // Return speed of the shield to its owner (units per second)
    private const float ReturnSpeed = 15.0f;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<ReturnableThrowingComponent, ThrownEvent>(OnThrown);
        SubscribeLocalEvent<ReturnableThrowingComponent, ThrowDoHitEvent>(OnHit);
        SubscribeLocalEvent<ReturnableThrowingComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
    }
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        
        // Update positions of all returning items
        var query = EntityQueryEnumerator<ReturnableThrowingComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.IsReturning || component.TargetEntity == null)
                continue;
                
            if (!_entityManager.EntityExists(component.TargetEntity.Value))
            {
                component.IsReturning = false;
                component.TargetEntity = null;
                continue;
            }
            
            // Get current shield position and target position
            var currentPos = _transform.GetWorldPosition(uid);
            var targetPos = _transform.GetWorldPosition(component.TargetEntity.Value);
            
            // Calculate direction and distance
            var direction = targetPos - currentPos;
            var distance = direction.Length();
            
            // If shield is close enough to target, finish movement
            if (distance < 0.2f)
            {
                FinishShieldReturn(uid, component);
                continue;
            }
            
            // Normalize direction and calculate new position
            direction = Vector2.Normalize(direction);
            var movement = direction * ReturnSpeed * frameTime;
            
            // If shield would move further than target in one frame, place it directly in hands
            if (movement.Length() >= distance)
            {
                FinishShieldReturn(uid, component);
                continue;
            }
            
            // Move shield to new position
            var newPos = currentPos + movement;
            _transform.SetWorldPosition(uid, newPos);
            
            // Rotate shield in direction of movement
            var angle = direction.ToWorldAngle();
            _transform.SetWorldRotation(uid, angle);
            
            // Disable physics while returning
            if (TryComp<PhysicsComponent>(uid, out var physics))
            {
                _physics.SetCanCollide(uid, false, body: physics);
            }
        }
    }
    
    private void OnThrown(EntityUid uid, ReturnableThrowingComponent component, ThrownEvent args)
    {
        if (args.User == null)
            return;
            
        // Save information about who threw the item
        component.LastThrower = args.User.Value;
    }
    
    private void OnHit(EntityUid uid, ReturnableThrowingComponent component, ThrowDoHitEvent args)
    {
        EntityUid? thrower = component.LastThrower;
        if (thrower == null && TryComp<ThrownItemComponent>(uid, out var thrownComp))
        {
            thrower = thrownComp.Thrower;
        }
        
        if (thrower == null || !EntityManager.EntityExists(thrower.Value))
            return;
        
        if (!HasComp<BloodCultistComponent>(thrower.Value))
            return;
            
        // Check if owner is alive
        if (TryComp<MobStateComponent>(thrower.Value, out var mobState) && 
            _mobStateSystem.IsDead(thrower.Value, mobState))
        {
            // If owner is dead, shield doesn't return
            return;
        }

        // Stop item flight on hit
        if (TryComp<ThrownItemComponent>(uid, out var thrown) && TryComp<PhysicsComponent>(uid, out var physics))
        {
            _thrownSystem.LandComponent(uid, thrown, physics, false);
        }

        bool isValidTarget = HasComp<MobStateComponent>(args.Target) && !HasComp<BloodCultistComponent>(args.Target);
        
        if (!isValidTarget)
            return;

        // Stun the target
        _stun.TryParalyze(args.Target, TimeSpan.FromSeconds(1f), true);

        // Get owner name
        string ownerName = MetaData(thrower.Value).EntityName;
        
        // Show return message only to owner
        var message = Loc.GetString(MessageReturnedToHands, ("name", ownerName));
        _popup.PopupEntity(message, thrower.Value, thrower.Value);
        
        // Initiate visual shield return
        StartReturnAnimation(uid, thrower.Value, component);
    }
    
    /// <summary>
    /// Starts animation of shield return directly to owner
    /// </summary>
    private void StartReturnAnimation(EntityUid uid, EntityUid thrower, ReturnableThrowingComponent component)
    {        
        // Set flag that shield is returning
        component.IsReturning = true;
        component.TargetEntity = thrower;
        
        // Disable physics components during animation
        if (TryComp<PhysicsComponent>(uid, out var physics))
        {
            _physics.SetCanCollide(uid, false, body: physics);
        }
        
        // Remove thrown item component so it doesn't interact with other objects
        if (HasComp<ThrownItemComponent>(uid))
        {
            RemComp<ThrownItemComponent>(uid);
        }
    }
    
    /// <summary>
    /// Completes shield return by placing it in owner's hands
    /// </summary>
    private void FinishShieldReturn(EntityUid uid, ReturnableThrowingComponent component)
    {
        if (component.TargetEntity == null || !_entityManager.EntityExists(component.TargetEntity.Value))
        {
            component.IsReturning = false;
            component.TargetEntity = null;
            return;
        }
        
        var thrower = component.TargetEntity.Value;
        
        // Reset return flags
        component.IsReturning = false;
        component.TargetEntity = null;
        
        // Check if owner has free hands
        if (!TryComp<HandsComponent>(thrower, out var hands))
            return;
            
        // Restore item physics
        if (TryComp<PhysicsComponent>(uid, out var physics))
        {
            _physics.SetCanCollide(uid, true, body: physics);
        }
        
        // Get owner name
        string ownerName = MetaData(thrower).EntityName;
            
        var freeHand = _hands.EnumerateHands(thrower)
            .FirstOrDefault(hand => hand.IsEmpty);

        if (freeHand != null)
        {
            // Try to pick up shield
            _transform.SetParent(uid, thrower);
            _hands.TryPickup(thrower, uid, freeHand.Name);
        }
        else
        {
            // If no free hands, place shield at owner's feet
            var throwerCoords = Transform(thrower).Coordinates;
            _transform.SetCoordinates(uid, throwerCoords);
            var dropMessage = Loc.GetString(MessageReturnedToFeet, ("name", ownerName));
            _popup.PopupEntity(dropMessage, thrower, thrower);
        }
    }

    private void OnPickupAttempt(EntityUid uid, ReturnableThrowingComponent component, GettingPickedUpAttemptEvent args)
    {
        // If shield is returning to owner, don't allow it to be picked up
        if (component.IsReturning)
        {
            args.Cancel();
            return;
        }
        
    }
} 