using Content.Server.CartridgeLoader;
using Content.Shared._NC.CitiNet;
using Content.Shared.CartridgeLoader;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Content.Server._NC.CitiNet.Cartridges;

/// <summary>
/// Server system for the CitiNet Map Cartridge.
/// Scans the grid for sectors and beacons to display on the layered map.
/// </summary>
public sealed class CitiNetMapCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<global::Content.Shared._NC.CitiNet.CitiNetMapCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<global::Content.Shared._NC.CitiNet.CitiNetMapCartridgeComponent, CartridgeMessageEvent>(OnMessage);
    }

    private void OnUiReady(Entity<global::Content.Shared._NC.CitiNet.CitiNetMapCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUI(ent, args.Loader);
    }

    private void OnMessage(Entity<global::Content.Shared._NC.CitiNet.CitiNetMapCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not global::Content.Shared._NC.CitiNet.CitiNetUiMessageEvent)
            return;

        UpdateUI(ent, GetEntity(args.LoaderUid));
    }

    private void UpdateUI(EntityUid uid, EntityUid loader)
    {
        var sectors = new List<Content.Shared._NC.CitiNet.CitiNetMapSectorData>();
        var beacons = new List<Content.Shared._NC.CitiNet.CitiNetMapBeaconData>();
        var pings = new List<Content.Shared._NC.CitiNet.CitiNetMapPingData>();

        // Get the grid we are currently on
        var xform = Transform(loader);
        var gridUid = xform.GridUid;

        if (gridUid != null)
        {
            // 1. Scan for Sectors
            var sectorQuery = EntityQueryEnumerator<Content.Shared._NC.CitiNet.MapSectorComponent, TransformComponent>();
            while (sectorQuery.MoveNext(out var sUid, out var sector, out var sXform))
            {
                if (sXform.GridUid != gridUid)
                    continue;

                sectors.Add(new Content.Shared._NC.CitiNet.CitiNetMapSectorData(
                    sector.SectorName,
                    sector.Color,
                    sector.Bounds
                ));
            }

            // 2. Scan for Beacons (POIs)
            var beaconQuery = EntityQueryEnumerator<Content.Shared._NC.CitiNet.MapBeaconComponent, TransformComponent>();
            while (beaconQuery.MoveNext(out var bUid, out var beacon, out var bXform))
            {
                if (bXform.GridUid != gridUid || !beacon.IsVisible)
                    continue;

                beacons.Add(new Content.Shared._NC.CitiNet.CitiNetMapBeaconData(
                    GetNetEntity(bUid),
                    beacon.Label,
                    beacon.Icon,
                    beacon.Color,
                    bXform.LocalPosition
                ));
            }

            // 3. Scan for Dynamic Pings
            var pingQuery = EntityQueryEnumerator<Content.Shared._NC.CitiNet.CitiNetPingComponent, TransformComponent>();
            while (pingQuery.MoveNext(out var pUid, out var ping, out var pXform))
            {
                if (pXform.GridUid != gridUid)
                    continue;

                pings.Add(new Content.Shared._NC.CitiNet.CitiNetMapPingData(
                    pXform.LocalPosition,
                    ping.Color,
                    ping.Radius
                ));
            }
        }

        var state = new global::Content.Shared._NC.CitiNet.CitiNetMapBoundUserInterfaceState(GetNetEntity(gridUid), sectors, beacons, pings);
        _cartridge.UpdateCartridgeUiState(loader, state);
    }
}
