using Content.Shared._White.ItemSlotPicker.UI;
using Content.Shared.ActionBlocker;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.ItemSlotPicker;

public abstract class SharedItemSlotPickerSystem : EntitySystem
{
    [Dependency] protected readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] protected readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] protected readonly ActionBlockerSystem _blocker = default!;
    [Dependency] protected readonly SharedInteractionSystem _interact = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ItemSlotPickerComponent, ComponentInit>(CompInit);
        SubscribeLocalEvent<ItemSlotPickerComponent, AlternativeInteractionEvent>(AltInteract);
        SubscribeLocalEvent<ItemSlotPickerComponent, ItemSlotPickerSlotPickedMessage>(OnMessage);
    }

    protected virtual void CompInit(EntityUid uid, ItemSlotPickerComponent comp, ComponentInit args)
    {
        _ui.SetUi(uid, ItemSlotPickerKey.Key, new InterfaceData("ItemSlotPickerBoundUserInterface", 1.5f));
    }

    protected virtual void AltInteract(EntityUid uid, ItemSlotPickerComponent comp, AlternativeInteractionEvent args)
    {
        var user = args.User;
        if (!TryComp<ItemSlotsComponent>(uid, out var slots) ||
            !TryComp<HandsComponent>(user, out var hands) ||
            !_blocker.CanComplexInteract(user) ||
            !_blocker.CanInteract(user, uid) ||
            !_interact.InRangeAndAccessible(user, uid, 1.5f))
            return;

        args.Handled = true;

        if (hands.ActiveHandEntity is EntityUid item)
            foreach (var slot in comp.ItemSlots)
                if (_itemSlots.TryInsert(uid, slot, item, user, slots, true))
                    return; // I wish this altverb bullshit wasn't a thing.

        _ui.TryToggleUi(uid, ItemSlotPickerKey.Key, user);
    }

    protected virtual void OnMessage(EntityUid uid, ItemSlotPickerComponent comp, ItemSlotPickerSlotPickedMessage args)
    {
        if (!comp.ItemSlots.Contains(args.SlotId) ||
            !_itemSlots.TryGetSlot(uid, args.SlotId, out var slot))
            return;

        _itemSlots.TryEjectToHands(uid, slot, args.Actor, true);
        _ui.CloseUi(uid, ItemSlotPickerKey.Key, args.Actor);
    }
}
[Serializable, NetSerializable]
public enum ItemSlotPickerKey { Key };
