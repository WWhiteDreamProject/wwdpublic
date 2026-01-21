using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Vehicle.Vendor;

[RegisterComponent, NetworkedComponent]
public sealed partial class VehicleVendorComponent : Component
{
    [DataField]
    public List<VehicleEntry> Vehicles = new();
}

[DataDefinition, Serializable, NetSerializable]
public partial struct VehicleEntry
{
    [DataField] public string ProtoId;
    [DataField] public string Name;
    [DataField] public int Price;

    public VehicleEntry(string protoId, string name, int price)
    {
        ProtoId = protoId;
        Name = name;
        Price = price;
    }
}

[Serializable, NetSerializable]
public enum VehicleVendorUiKey
{
    Key
}
