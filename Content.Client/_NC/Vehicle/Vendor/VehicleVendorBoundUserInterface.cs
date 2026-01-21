// NC
using Content.Shared._NC.Vehicle.Vendor;
using Robust.Client.GameObjects;

namespace Content.Client._NC.Vehicle.Vendor;

public sealed class VehicleVendorBoundUserInterface : BoundUserInterface
{
    private VehicleVendorMenu? _menu;

    public VehicleVendorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _menu = new VehicleVendorMenu(this);
        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is VehicleVendorBoundUserInterfaceState vState)
        {
            _menu?.UpdateVehicles(vState.Vehicles);
        }
    }

    public void Buy(string protoId)
    {
        SendMessage(new VehicleVendorBuyMessage(protoId));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _menu?.Dispose();
    }
}
