using Robust.Shared.Serialization;


namespace Content.Shared._NC.Vehicle.Vendor;

[Serializable, NetSerializable]
public sealed class VehicleVendorBoundUserInterfaceState : BoundUserInterfaceState
{
    public List<VehicleEntry> Vehicles;

    public VehicleVendorBoundUserInterfaceState(List<VehicleEntry> vehicles)
    {
        Vehicles = vehicles;
    }
}

[Serializable, NetSerializable]
public sealed class VehicleVendorBuyMessage : BoundUserInterfaceMessage
{
    public string ProtoId;

    public VehicleVendorBuyMessage(string protoId)
    {
        ProtoId = protoId;
    }
}
