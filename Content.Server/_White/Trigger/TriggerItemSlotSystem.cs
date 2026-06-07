using Content.Server.Explosion.EntitySystems;
using Content.Shared._White.Trigger;
using Content.Shared.Containers.ItemSlots;
using Robust.Server.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._White.Trigger;

public sealed class TriggerItemSlotSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TriggerItemSlotComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(EntityUid uid, TriggerItemSlotComponent component, ref TriggerEvent args)
    {
        if (!TryComp<ItemSlotsComponent>(uid, out var itemSlotsComp))
            return;

        foreach (var slotName in component.Slots)
        {
            if (!_itemSlots.TryGetSlot(uid, slotName, out var slot, itemSlotsComp))
                continue;

            if (slot.Item is EntityUid containedItem)
            {
                if (!_itemSlots.TryEjectToHands(uid, slot, null))
                {
                    _xform.AttachToGridOrMap(containedItem);
                }

                _trigger.Trigger(containedItem, args.User);
            }
        }
    }
}
