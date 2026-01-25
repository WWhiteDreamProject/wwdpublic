using Content.Shared._NC.Doors.Components;
using Content.Shared._NC.Doors;
using Content.Server.Doors.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using System.Linq;
using Robust.Shared.Player;

namespace Content.Server._NC.Doors.Systems;

public sealed class DoorPurchaseSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DoorPurchaseConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<DoorPurchaseConsoleComponent, DoorPurchaseConsoleOpenInterfaceMessage>(OnOpenInterface);
    }

    private void OnUiOpened(EntityUid uid, DoorPurchaseConsoleComponent component, BoundUIOpenedEvent args)
    {
        var xform = Transform(uid);
        var mapUid = xform.MapUid;
        var gridUid = xform.GridUid;

        // Use the grid if available, otherwise map
        var targetMapEntity = gridUid ?? mapUid;

        // Find all doors with DoorInterfaceComponent
        var query = EntityQueryEnumerator<DoorInterfaceComponent, TransformComponent>();
        var properties = new System.Collections.Generic.List<(NetEntity, NetCoordinates, int, string)>();

        while (query.MoveNext(out var doorUid, out var doorInterface, out var doorXform))
        {
            // Filter by map/grid to show only local properties
            if (doorXform.MapUid != mapUid)
                continue;

            // Only show unowned properties
            if (doorInterface.OwnerId != null)
                continue;

            // Ensure coordinates are relative to the map/grid we are displaying
            var netCoords = GetNetCoordinates(doorXform.Coordinates);
            var netUid = GetNetEntity(doorUid);
            string doorCode = doorInterface.DoorCode ?? "???";

            properties.Add((netUid, netCoords, doorInterface.Price, doorCode));
        }

        var netMapEntity = GetNetEntity(targetMapEntity);
        var state = new DoorPurchaseConsoleState(properties, netMapEntity);
        _uiSystem.SetUiState(uid, DoorPurchaseConsoleUiKey.Key, state);
    }

    private void OnOpenInterface(EntityUid uid, DoorPurchaseConsoleComponent component, DoorPurchaseConsoleOpenInterfaceMessage args)
    {
        if (args.Actor is not { Valid: true } user)
            return;

        if (!_playerManager.TryGetSessionByEntity(user, out var session))
            return;

        var doorUid = GetEntity(args.DoorUid);

        if (!Exists(doorUid) || !HasComp<DoorInterfaceComponent>(doorUid))
            return;

        if (TryComp<Content.Server.Power.Components.ApcPowerReceiverComponent>(doorUid, out var power) && !power.Powered)
            return;

        // Open the door's UI for the user remote-like, forcing it open.
        // We MUST ensure the user has IgnoreUIRangeComponent otherwise the interaction system will Assert/Fail immediately upon OpenUi logic.
        EnsureComp<Robust.Shared.GameObjects.IgnoreUIRangeComponent>(user);

        _uiSystem.OpenUi(doorUid, DoorInterfaceUiKey.Key, session);
    }
}
