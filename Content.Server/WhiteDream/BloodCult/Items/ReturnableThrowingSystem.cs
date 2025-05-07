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

    private const string MessageKey = "returnable-cult-shield-pickup-fail";
    private const string MessageReturnedToHands = "returnable-cult-item-returned-to-hands";
    private const string MessageReturnedToFeet = "returnable-cult-item-returned-to-feet";

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<ReturnableThrowingComponent, BeforeGettingThrownEvent>(OnThrow);
        SubscribeLocalEvent<ReturnableThrowingComponent, ThrownEvent>(OnThrown);
        SubscribeLocalEvent<ReturnableThrowingComponent, ThrowDoHitEvent>(OnHit);
        SubscribeLocalEvent<ReturnableThrowingComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<ReturnableThrowingComponent, ActivateInWorldEvent>(OnActivateInWorldAttempt);
        SubscribeLocalEvent<ReturnableThrowingComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ReturnableThrowingComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
    }

    private void OnActivateInWorldAttempt(EntityUid uid, ReturnableThrowingComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<CultItemComponent>(uid, out var cultItem))
            return;

        if (!HasComp<BloodCultistComponent>(args.User) && !cultItem.AllowUseToEveryone)
        {
            args.Handled = true;
            var message = Loc.GetString(MessageKey, ("item", uid));
            _popup.PopupEntity(message, uid, args.User);
        }
    }
    
    private void OnUseInHand(EntityUid uid, ReturnableThrowingComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<CultItemComponent>(uid, out var cultItem))
            return;
            
        if (HasComp<BloodCultistComponent>(args.User) || cultItem.AllowUseToEveryone)
            return;
            
        args.Handled = true;
        var message = Loc.GetString(MessageKey, ("item", uid));
        _popup.PopupEntity(message, uid, args.User);
    }
    
    private void OnEquipAttempt(EntityUid uid, ReturnableThrowingComponent component, BeingEquippedAttemptEvent args)
    {
        if (!TryComp<CultItemComponent>(uid, out var cultItem))
            return;
            
        if (HasComp<BloodCultistComponent>(args.EquipTarget) || cultItem.AllowUseToEveryone)
            return;
            
        args.Cancel();
        var message = Loc.GetString(MessageKey, ("item", uid));
        _popup.PopupEntity(message, uid, args.Equipee);
    }

    private void OnThrow(EntityUid uid, ReturnableThrowingComponent component, BeforeGettingThrownEvent args)
    {
        component.LastThrower = args.PlayerUid;
    }
    
    private void OnThrown(EntityUid uid, ReturnableThrowingComponent component, ThrownEvent args)
    {
        component.LastThrower = args.User;
    }

    private void OnHit(EntityUid uid, ReturnableThrowingComponent component, ThrowDoHitEvent args)
    {
        if (!TryComp<CultItemComponent>(uid, out var cultItem))
            return;

        EntityUid? thrower = component.LastThrower;
        if (thrower == null && TryComp<ThrownItemComponent>(uid, out var thrownComp))
        {
            thrower = thrownComp.Thrower;
        }
        
        if (thrower == null || !EntityManager.EntityExists(thrower.Value))
            return;
        
        if (!HasComp<BloodCultistComponent>(thrower.Value))
            return;

        // Stop the movement of the shield on any hit
        if (TryComp<ThrownItemComponent>(uid, out var thrown) && TryComp<PhysicsComponent>(uid, out var physics))
        {
            _thrownSystem.LandComponent(uid, thrown, physics, false, false);
        }

        // Check that the target is a creature and not a cultist
        bool isValidTarget = HasComp<MobStateComponent>(args.Target) && !HasComp<BloodCultistComponent>(args.Target);
        
        // If the target is not a non-cultist creature, do not return the shield
        if (!isValidTarget)
            return;

        // Stun the target
        _stun.TryParalyze(args.Target, TimeSpan.FromSeconds(1f), true);

        // Return the shield to the cultist
        if (!TryComp<HandsComponent>(thrower.Value, out var hands))
            return;

        var freeHand = _hands.EnumerateHands(thrower.Value)
            .FirstOrDefault(hand => hand.IsEmpty);

        if (freeHand != null)
        {
            _transform.SetParent(uid, thrower.Value);
            _hands.TryPickup(thrower.Value, uid, freeHand.Name);
            var message = Loc.GetString(MessageReturnedToHands);
            _popup.PopupEntity(message, thrower.Value, thrower.Value);
        }
        else
        {
            var throwerPos = Transform(thrower.Value).Coordinates;
            _transform.SetCoordinates(uid, throwerPos);
            var message = Loc.GetString(MessageReturnedToFeet);
            _popup.PopupEntity(message, thrower.Value, thrower.Value);
        }
    }

    private void OnPickupAttempt(EntityUid uid, ReturnableThrowingComponent component, GettingPickedUpAttemptEvent args)
    {
        if (!TryComp<CultItemComponent>(uid, out var cultItem))
            return;

        if (!HasComp<BloodCultistComponent>(args.User) && !cultItem.AllowUseToEveryone)
        {
            args.Cancel();
            var message = Loc.GetString(MessageKey, ("item", uid));
            _popup.PopupEntity(message, args.Item, args.User);
        }
    }
} 