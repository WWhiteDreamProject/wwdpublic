using System.Linq;
using Content.Shared.Examine;
using Content.Shared._White.Hands.Components;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Localizations;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem : EntitySystem
{
    private void InitializeInteractions()
    {
        SubscribeAllEvent<RequestSetHandEvent>(HandleSetHand);
        SubscribeAllEvent<RequestActivateInHandEvent>(HandleActivateItemInHand);
        SubscribeAllEvent<RequestHandInteractUsingEvent>(HandleInteractUsingInHand);
        SubscribeAllEvent<RequestUseInHandEvent>(HandleUseInHand);
        SubscribeAllEvent<RequestMoveHandItemEvent>(HandleMoveItemFromHand);
        SubscribeAllEvent<RequestHandAltInteractEvent>(HandleHandAltInteract);

        SubscribeLocalEvent<HandsComponent, GetUsedEntityEvent>(OnGetUsedEntity);
        SubscribeLocalEvent<HandsComponent, ExaminedEvent>(HandleExamined);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.UseItemInHand, InputCmdHandler.FromDelegate(HandleUseItem, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.AltUseItemInHand, InputCmdHandler.FromDelegate(HandleAltUseInHand, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.SwapHands, InputCmdHandler.FromDelegate(SwapHandsPressed, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.Drop, new PointerInputCmdHandler(DropPressed))
			// WWDP EDIT START
            .Bind(ContentKeyFunctions.PreciseDrop, new PointerInputCmdHandler(PreciseDropButton, false, true))
            .Bind(ContentKeyFunctions.MouseWheelUp, new PointerInputCmdHandler(PreciseDropMWheelUp))
            .Bind(ContentKeyFunctions.MouseWheelDown, new PointerInputCmdHandler(PreciseDropMWheelDown))
            .Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(PreciseDropCancel))
            .Bind(EngineKeyFunctions.UseSecondary, new PointerInputCmdHandler(PreciseDropCancel))
            // WWDP EDIT END
            .Register<SharedHandsSystem>();
    }
	// WWDP EDIT START
    private bool PreciseDropCancel(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.Session?.AttachedEntity is EntityUid uid && HasComp<HoldingDropComponent>(uid))
            RemComp<HoldingDropComponent>(uid);
        return false;
    }

    private bool PreciseDropButton(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.Session?.AttachedEntity is not EntityUid uid)
            return false;

        if(!TryComp<HandsComponent>(uid, out var hands) ||
            hands.ActiveHandEntity == null)
        {
            if (HasComp<HoldingDropComponent>(uid))
                RemComp<HoldingDropComponent>(uid);
            return false;
        }

        if(args.State == BoundKeyState.Down)
            return PreciseDropButtonDown(uid, hands, args);
        return PreciseDropButtonUp(uid, hands, args);
    }

    private bool PreciseDropButtonDown(EntityUid uid, HandsComponent hands, in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        var comp = EnsureComp<HoldingDropComponent>(uid);
        return false;
    }

    private bool PreciseDropButtonUp(EntityUid uid, HandsComponent hands, in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (!TryComp<HoldingDropComponent>(uid, out var comp))
            return false;
        TryDrop(uid, hands.ActiveHand!, args.Coordinates, handsComp: hands, dropAngle: comp.Angle);
        RemComp<HoldingDropComponent>(uid);
        return false;
    }
	
    private readonly Angle _dropRotationIncrement = Angle.FromDegrees(5);
    private bool PreciseDropMWheelUp(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.Session?.AttachedEntity is not EntityUid uid ||
            !TryComp<HandsComponent>(uid, out var hands))
            return false;

        if (!TryComp<HoldingDropComponent>(uid, out var comp))
            return false;

        comp.Angle += _dropRotationIncrement;
        Dirty(uid, comp);
        return false;
    }

    private bool PreciseDropMWheelDown(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.Session?.AttachedEntity is not EntityUid uid ||
            !TryComp<HandsComponent>(uid, out var hands))
            return false;

        if (!TryComp<HoldingDropComponent>(uid, out var comp))
            return false;

        comp.Angle -= _dropRotationIncrement;
        Dirty(uid, comp);
        return false;
    }
	// WWDP EDIT END
    #region Event and Key-binding Handlers
    private void HandleAltUseInHand(ICommonSession? session)
    {
        if (session?.AttachedEntity != null)
            TryUseItemInHand(session.AttachedEntity.Value, true);
    }

    private void HandleUseItem(ICommonSession? session)
    {
        if (session?.AttachedEntity != null)
            TryUseItemInHand(session.AttachedEntity.Value);
    }

    private void HandleMoveItemFromHand(RequestMoveHandItemEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity != null)
            TryMoveHeldEntityToActiveHand(args.SenderSession.AttachedEntity.Value, msg.HandName);
    }

    private void HandleUseInHand(RequestUseInHandEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity != null)
            TryUseItemInHand(args.SenderSession.AttachedEntity.Value);
    }

    private void HandleActivateItemInHand(RequestActivateInHandEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity != null)
            TryActivateItemInHand(args.SenderSession.AttachedEntity.Value, null, msg.HandName);
    }

    private void HandleInteractUsingInHand(RequestHandInteractUsingEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity != null)
            TryInteractHandWithActiveHand(args.SenderSession.AttachedEntity.Value, msg.HandName);
    }

    private void HandleHandAltInteract(RequestHandAltInteractEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity != null)
            TryUseItemInHand(args.SenderSession.AttachedEntity.Value, true, handName: msg.HandName);
    }

    private void SwapHandsPressed(ICommonSession? session)
    {
        if (!TryComp(session?.AttachedEntity, out HandsComponent? component))
            return;

        if (!_actionBlocker.CanInteract(session.AttachedEntity.Value, null))
            return;

        if (component.ActiveHand == null || component.Hands.Count < 2)
            return;

        var newActiveIndex = component.SortedHands.IndexOf(component.ActiveHand.Name) + 1;
        var nextHand = component.SortedHands[newActiveIndex % component.Hands.Count];

        TrySetActiveHand(session.AttachedEntity.Value, nextHand, component);
    }

    private bool DropPressed(ICommonSession? session, EntityCoordinates coords, EntityUid netEntity)
    {
        if (TryComp(session?.AttachedEntity, out HandsComponent? hands) && hands.ActiveHand != null)
            TryDrop(session.AttachedEntity.Value, hands.ActiveHand, coords, handsComp: hands);

        // always send to server.
        return false;
    }
    #endregion

    public bool TryActivateItemInHand(EntityUid uid, HandsComponent? handsComp = null, string? handName = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        Hand? hand;
        if (handName == null || !handsComp.Hands.TryGetValue(handName, out hand))
            hand = handsComp.ActiveHand;

        if (hand?.HeldEntity is not { } held)
            return false;

        return _interactionSystem.InteractionActivate(uid, held);
    }

    public bool TryInteractHandWithActiveHand(EntityUid uid, string handName, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        if (handsComp.ActiveHandEntity == null)
            return false;

        if (!handsComp.Hands.TryGetValue(handName, out var hand))
            return false;

        if (hand.HeldEntity == null)
            return false;

        _interactionSystem.InteractUsing(uid, handsComp.ActiveHandEntity.Value, hand.HeldEntity.Value, Transform(hand.HeldEntity.Value).Coordinates);
        return true;
    }

    public bool TryUseItemInHand(EntityUid uid, bool altInteract = false, HandsComponent? handsComp = null, string? handName = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        Hand? hand;
        if (handName == null || !handsComp.Hands.TryGetValue(handName, out hand))
            hand = handsComp.ActiveHand;

        if (hand?.HeldEntity is not { } held)
            return false;

        if (altInteract)
            if(hand == handsComp.ActiveHand)    // WD EDIT START
                return _interactionSystem.ActiveHandAltInteract(uid, held) || _interactionSystem.AltInteract(uid, held); // todo: should these be merged into one method?
            else                                // WD EDIT END
                return _interactionSystem.AltInteract(uid, held);
        else
            return _interactionSystem.UseInHandInteraction(uid, held);
    }

    /// <summary>
    ///     Moves an entity from one hand to the active hand.
    /// </summary>
    public bool TryMoveHeldEntityToActiveHand(EntityUid uid, string handName, bool checkActionBlocker = true, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp))
            return false;

        if (handsComp.ActiveHand == null || !handsComp.ActiveHand.IsEmpty)
            return false;

        if (!handsComp.Hands.TryGetValue(handName, out var hand))
            return false;

        if (!CanDropHeld(uid, hand, checkActionBlocker))
            return false;

        var entity = hand.HeldEntity!.Value;

        if (!CanPickupToHand(uid, entity, handsComp.ActiveHand, checkActionBlocker, handsComp))
            return false;

        DoDrop(uid, hand, false, handsComp);
        DoPickup(uid, handsComp.ActiveHand, entity, handsComp);
        return true;
    }

    private void OnGetUsedEntity(EntityUid uid, HandsComponent component, ref GetUsedEntityEvent args)
    {
        if (args.Handled)
            return;

        // TODO: this pattern is super uncommon, but it might be worth changing GetUsedEntityEvent to be recursive.
        if (TryComp<VirtualItemComponent>(component.ActiveHandEntity, out var virtualItem))
            args.Used = virtualItem.BlockingEntity;
        else
            args.Used = component.ActiveHandEntity;
    }

    //TODO: Actually shows all items/clothing/etc.
    private void HandleExamined(EntityUid examinedUid, HandsComponent handsComp, ExaminedEvent args)
    {
        var heldItemNames = EnumerateHeld(examinedUid, handsComp)
            .Where(entity => !HasComp<VirtualItemComponent>(entity))
            .Select(item => FormattedMessage.EscapeText(Identity.Name(item, EntityManager)))
            .Select(itemName => Loc.GetString("comp-hands-examine-wrapper", ("item", itemName)))
            .ToList();

        var locKey = heldItemNames.Count != 0 ? "comp-hands-examine" : "comp-hands-examine-empty";
        var locUser = ("user", Identity.Entity(examinedUid, EntityManager));
        var locItems = ("items", ContentLocalizationManager.FormatList(heldItemNames));

        // WWDP examine
        if (args.Examiner == args.Examined) // Use the selfaware locale when inspecting yourself
            locKey += "-selfaware";

        using (args.PushGroup(nameof(HandsComponent), 99)) //  priority for examine
        {
            args.PushMarkup("- " + Loc.GetString(locKey, locUser, locItems)); // "-" for better formatting
        }
        // WWDP edit end
    }
}

