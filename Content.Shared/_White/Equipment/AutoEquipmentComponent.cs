using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Roles;

namespace Content.Shared.Equipment.Components;

[RegisterComponent]
public sealed partial class AutoEquipmentComponent : Component
{
    [DataField]
    public List<ProtoId<StartingGearPrototype>> StartingGears = new();

    [DataField]
    public bool ForceEquip = false;

    [DataField]
    public float DoAfterDelay = 0f;

    [DataField]
    public bool BreakOnMove = false;

    [DataField]
    public bool DeleteOldItems = false;

    [DataField]
    public bool ClearAllEquipment = false;
}
