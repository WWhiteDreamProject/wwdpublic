using System;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Shared._NC.Doors.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)] // NC
public sealed partial class DoorInterfaceComponent : Component
{
    [DataField("price")]
    public int Price = 1000;

    [DataField("ownerName")]
    public string? OwnerName;

    [DataField("ownerId")]
    public Guid? OwnerId;

    [DataField("address")]
    public string Address = "Квартира";

    [DataField, AutoNetworkedField]
    public string? DoorCode; // NC
}

[Serializable, NetSerializable]
public sealed class DoorInterfaceState : BoundUserInterfaceState
{
    public int Price { get; }
    public string? OwnerName { get; }
    public string Address { get; }
    public Guid? OwnerId { get; }
    public bool IsLocked { get; }
    public string? DoorCode { get; }

    public DoorInterfaceState(int price, string? ownerName, string address, Guid? ownerId, bool isLocked, string? doorCode)
    {
        Price = price;
        OwnerName = ownerName;
        Address = address;
        OwnerId = ownerId;
        IsLocked = isLocked;
        DoorCode = doorCode;
    }
}

[Serializable, NetSerializable]
public sealed class DoorInterfaceBuyMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class DoorInterfaceSellMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class DoorInterfaceLockMessage : BoundUserInterfaceMessage
{
}
