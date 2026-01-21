using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.PDA;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Content.Shared._NC.Vehicle;
using Content.Shared._NC.Vehicle.Vendor; // Will update component/events to this namespace next
using Robust.Server.GameObjects;

namespace Content.Server._NC.Vehicle.Vendor;

public sealed class VehicleVendorSystem : EntitySystem
{
    [Dependency] private readonly NCVehicleSystem _ncVehicle = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VehicleVendorComponent, VehicleVendorBuyMessage>(OnBuy);
        SubscribeLocalEvent<VehicleVendorComponent, BoundUIOpenedEvent>(OnOpen);
    }

    private void OnOpen(EntityUid uid, VehicleVendorComponent component, BoundUIOpenedEvent args)
    {
        _ui.SetUiState(uid, VehicleVendorUiKey.Key, new VehicleVendorBoundUserInterfaceState(component.Vehicles));
    }

    private void OnBuy(EntityUid uid, VehicleVendorComponent component, VehicleVendorBuyMessage args)
    {
        var user = args.Actor;

        // Find entry
        var entry = component.Vehicles.Find(x => x.ProtoId == args.ProtoId);
        if (entry.Equals(default(VehicleEntry)))
            return;

        // TODO: Credit check (skipped for now as per plan)

        // Spawn Vehicle
        var vehicle = Spawn(entry.ProtoId, Transform(uid).Coordinates);

        // Personalize
        _ncVehicle.PersonalizeVehicle(vehicle, user);

        _popup.PopupEntity($"Purchased {entry.Name}!", uid, user);
    }
}
