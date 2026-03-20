using Content.Shared.Equipment.Components;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction.Events;
using Content.Shared.Equipment.Events;
using Content.Shared.DoAfter;
using Robust.Shared.Network;
using Robust.Shared.GameStates;
using System.Linq;

namespace Content.Shared.Equipment.Systems;

public sealed class AutoEquipmentSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    private readonly HashSet<EntityUid> _activeEquipment = new();
    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _predictedItems = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutoEquipmentComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<AutoEquipmentComponent, AutoEquipmentDoAfterEvent>(OnDoAfterComplete);
        SubscribeLocalEvent<AutoEquipmentComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, AutoEquipmentComponent component, ref ComponentHandleState args)
    {
        if (_netManager.IsClient && _predictedItems.TryGetValue(uid, out var predicted))
        {
            foreach (var item in predicted)
            {
                if (Exists(item) && MetaData(item).EntityLifeStage < EntityLifeStage.Terminating)
                {
                    QueueDel(item);
                }
            }
            _predictedItems.Remove(uid);
        }
    }

    private void OnUseInHand(EntityUid uid, AutoEquipmentComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (_netManager.IsClient && _activeEquipment.Contains(uid))
            return;

        args.Handled = true;

        if (_netManager.IsClient)
            _activeEquipment.Add(uid);

        if (component.DoAfterDelay <= 0)
        {
            EquipItemsAndDelete(args.User, uid, component);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(component.DoAfterDelay),
            new AutoEquipmentDoAfterEvent(), uid, used: uid)
        {
            BreakOnMove = component.BreakOnMove,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfterComplete(EntityUid uid, AutoEquipmentComponent component, AutoEquipmentDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        _activeEquipment.Remove(uid);
        EquipItemsAndDelete(args.User, uid, component);
        args.Handled = true;
    }

    private void EquipItemsAndDelete(EntityUid user, EntityUid equipmentUid, AutoEquipmentComponent component)
    {
        if (_netManager.IsClient && component.DoAfterDelay > 0)
            return;

        EquipItems(user, component);

        if (_netManager.IsClient)
            return;
        QueueDel(equipmentUid);
    }

    public void EquipItems(EntityUid user, AutoEquipmentComponent component)
    {
        if (!TryComp<InventoryComponent>(user, out var inventory))
            return;

        var userCoords = _transform.GetMapCoordinates(user);

        if (component.ClearAllEquipment)
        {
            ClearAllEquipment(user, inventory);
        }

        var spawnedItems = new HashSet<EntityUid>();

        var slotOrder = new Dictionary<string, int>
        {
            { "jumpsuit", 0 },
            { "outerClothing", 1 },
            { "suitstorage", 2 },
            { "shoes", 3 },
            { "back", 4 },
            { "head", 5 },
            { "mask", 6 },
            { "eyes", 7 },
            { "ears", 8 },
            { "neck", 9 },
            { "gloves", 10 },
            { "belt", 11 },
            { "id", 12 },
            { "pocket1", 13 },
            { "pocket2", 14 },
            { "pocket3", 15 },
            { "pocket4", 16 }
        };

        var orderedSlots = component.EquipmentSlots
            .Where(x => slotOrder.ContainsKey(x.Key))
            .OrderBy(x => slotOrder[x.Key])
            .ToList();

        foreach (var (slotName, itemProto) in orderedSlots)
        {
            if (!_inventory.HasSlot(user, slotName, inventory))
                continue;

            if (component.ForceEquip && _inventory.TryGetSlotEntity(user, slotName, out var existingItem, inventory))
            {
                if (component.DeleteOldItems)
                {
                    DeleteSlotWithDependencies(user, slotName, existingItem.Value, inventory);
                }
                else
                {
                    _inventory.TryUnequip(user, slotName, silent: true, force: true, inventory: inventory);
                }
            }

            var newItem = EntityManager.SpawnEntity(itemProto, userCoords);
            spawnedItems.Add(newItem);

            var equipResult = _inventory.TryEquip(user, newItem, slotName, silent: true, force: component.ForceEquip, inventory: inventory);

            if (!equipResult)
            {
                QueueDel(newItem);
            }
        }
    }

    private void ClearAllEquipment(EntityUid user, InventoryComponent inventory)
    {
        var slotsToRemove = new List<string>();

        var slotOrder = new Dictionary<string, int>
        {
            { "pocket4", 0 }, { "pocket3", 1 }, { "pocket2", 2 }, { "pocket1", 3 },
            { "id", 4 }, { "belt", 5 }, { "gloves", 6 }, { "neck", 7 },
            { "ears", 8 }, { "eyes", 9 }, { "mask", 10 }, { "head", 11 },
            { "back", 12 }, { "shoes", 13 }, { "suitstorage", 14 },
            { "outerClothing", 15 }, { "jumpsuit", 16 }
        };

        foreach (var slot in inventory.Slots)
        {
            if (_inventory.TryGetSlotEntity(user, slot.Name, out _, inventory))
            {
                slotsToRemove.Add(slot.Name);
            }
        }

        var orderedSlots = slotsToRemove
            .Where(x => slotOrder.ContainsKey(x))
            .OrderByDescending(x => slotOrder[x])
            .ToList();

        foreach (var slotName in orderedSlots)
        {
            if (_inventory.TryGetSlotEntity(user, slotName, out var item, inventory))
            {
                DeleteSlotWithDependencies(user, slotName, item.Value, inventory);
            }
        }
    }

    private void DeleteSlotWithDependencies(EntityUid user, string slotName, EntityUid item, InventoryComponent inventory)
    {
        if (slotName == "outerClothing")
        {
            var dependentSlots = new[] { "suitstorage" };
            foreach (var dependentSlot in dependentSlots)
            {
                if (_inventory.TryGetSlotEntity(user, dependentSlot, out var dependentItem, inventory))
                {
                    _inventory.TryUnequip(user, dependentSlot, silent: true, force: true, inventory: inventory);
                    QueueDel(dependentItem.Value);
                }
            }
        }
        else if (slotName == "jumpsuit")
        {
            var dependentSlots = new[] { "pocket1", "pocket2", "id" };
            foreach (var dependentSlot in dependentSlots)
            {
                if (_inventory.TryGetSlotEntity(user, dependentSlot, out var dependentItem, inventory))
                {
                    _inventory.TryUnequip(user, dependentSlot, silent: true, force: true, inventory: inventory);
                    QueueDel(dependentItem.Value);
                }
            }
        }

        _inventory.TryUnequip(user, slotName, silent: true, force: true, inventory: inventory);
        QueueDel(item);
    }
}
