using Content.Shared._White.Inventory.Components;
using Content.Shared.Inventory.Events;
using Robust.Shared.Physics.Events;


namespace Content.Shared._White.Inventory;

public sealed partial class WhiteInventorySystem
{
    private void InitializeEquip()
    {
        SubscribeLocalEvent<EquipOnCollideComponent, StartCollideEvent>(OnCollideEvent);
    }

    private void OnCollideEvent(EntityUid uid, EquipOnCollideComponent component, StartCollideEvent args)
    {

    }
}
