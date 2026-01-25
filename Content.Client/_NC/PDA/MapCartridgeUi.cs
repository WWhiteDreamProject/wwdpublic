using Content.Client.Pinpointer.UI;
using Content.Client.UserInterface.Fragments;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Map; // EntityCoordinates
using System.Numerics; // Vector2
using System.Linq;

namespace Content.Client._NC.PDA;

public sealed partial class MapCartridgeUi : UIFragment
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly Robust.Client.Player.IPlayerManager _playerManager = default!;

    private NavMapControl? _navMap;

    public override Control GetUIFragmentRoot()
    {
        return _rootContainer!;
    }

    private Control? _rootContainer;

    private EntityUid? _fragmentOwner;

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        IoCManager.InjectDependencies(this);
        _fragmentOwner = fragmentOwner;

        // Force fixed size to avoid overflow in PDA window (576px width)
        _navMap = new NavMapControl
        {
            // Do not expand, use fixed size
            HorizontalExpand = false,
            VerticalExpand = false
        };
        _navMap.SetWidth = 530f;
        _navMap.SetHeight = 380f;

        _rootContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            RectClipContent = true,
            Children = { _navMap }
        };

        // Hide internal header if possible to save space
        foreach (var child in _navMap.Children)
        {
            if (child is BoxContainer box)
            {
                if (box.ChildCount > 0 && box.GetChild(0) is PanelContainer panel)
                    panel.Visible = false;
                break;
            }
        }

        UpdateMap();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        UpdateMap();
    }

    private void UpdateMap()
    {
        if (_navMap == null || _fragmentOwner == null)
            return;

        // Try to find the grid the player is on
        var player = _playerManager.LocalSession?.AttachedEntity;
        if (player == null)
            return;

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        if (!xformQuery.TryGetComponent(player, out var xform))
            return;

        var gridUid = xform.GridUid;

        // Only update if changed
        if (_navMap.MapUid != gridUid)
        {
            _navMap.MapUid = gridUid;
            _navMap.ForceNavMapUpdate();
        }

        // Reset trackers
        _navMap.TrackedCoordinates.Clear();
        _navMap.CustomBeacons.Clear();
        _navMap.ClientBeaconsEnabled = true;

        // 1. Track the player (Red Dot)
        var playerCoords = new EntityCoordinates(player.Value, Vector2.Zero);
        _navMap.TrackedCoordinates.Add(playerCoords, (true, Robust.Shared.Maths.Color.Red));

        // 2. Resolve PDA data (Housing/Vehicle) - Read from Component
        if (!xformQuery.TryGetComponent(_fragmentOwner, out var cartridgeXform))
            return;

        var pdaUid = cartridgeXform.ParentUid;
        var pda = _entManager.GetComponent<Content.Shared.PDA.PdaComponent>(pdaUid);

        // 3. Track Housing (Yellow Code)
        if (!string.IsNullOrEmpty(pda.HousingName))
        {
            var codes = pda.HousingName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var query = _entManager.AllEntityQueryEnumerator<Content.Shared._NC.Doors.Components.DoorInterfaceComponent>();

            while (query.MoveNext(out var uid, out var door))
            {
                if (door.DoorCode != null && codes.Contains(door.DoorCode))
                {
                    var coords = new EntityCoordinates(uid, Vector2.Zero);
                    _navMap.CustomBeacons.Add((coords, door.DoorCode, Robust.Shared.Maths.Color.Yellow));
                }
            }
        }

        // 4. Track Vehicle (Blue Code)
        if (!string.IsNullOrEmpty(pda.VehicleId)) // VehicleId matches OwnerPlate
        {
            var query = _entManager.AllEntityQueryEnumerator<Content.Shared._NC.Vehicle.NCVehicleComponent>();
            while (query.MoveNext(out var uid, out var vehicle))
            {
                if (vehicle.OwnerPlate != null && vehicle.OwnerPlate == pda.VehicleId)
                {
                    var coords = new EntityCoordinates(uid, Vector2.Zero);
                    _navMap.CustomBeacons.Add((coords, vehicle.OwnerPlate, Robust.Shared.Maths.Color.Cyan));
                }
            }
        }
    }
}
