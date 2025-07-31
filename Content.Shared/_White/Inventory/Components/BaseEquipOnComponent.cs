using Content.Shared.Inventory;

namespace Content.Shared._White.Inventory.Components;

[RegisterComponent]
public abstract partial class BaseEquipOnComponent : Component
{
    [DataField(required: true)]
    public SlotFlags Slots = SlotFlags.NONE;

    [DataField]
    public float EquipProb = 1f;

    [DataField]
    public bool Force;

}
