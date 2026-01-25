using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace Content.Shared._NC.Doors.Components;

[Serializable, NetSerializable]
public sealed class DoorPurchaseConsoleState : BoundUserInterfaceState
{
    public List<(NetEntity Uid, NetCoordinates Coordinates, int Price, string DoorCode)> Properties { get; }
    public NetEntity? MapEntity { get; }

    public DoorPurchaseConsoleState(List<(NetEntity, NetCoordinates, int, string)> properties, NetEntity? mapEntity)
    {
        Properties = properties;
        MapEntity = mapEntity;
    }
}

[Serializable, NetSerializable]
public sealed class DoorPurchaseConsoleOpenInterfaceMessage : BoundUserInterfaceMessage
{
    public NetEntity DoorUid { get; }

    public DoorPurchaseConsoleOpenInterfaceMessage(NetEntity doorUid)
    {
        DoorUid = doorUid;
    }
}
