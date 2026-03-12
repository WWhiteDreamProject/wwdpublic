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

        SubscribeLocalEvent<CitiNetMapCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<CitiNetMapCartridgeComponent, CartridgeMessageEvent>(OnMessage);
    }

    private void OnUiReady(Entity<CitiNetMapCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUI(ent, args.Loader);
    }

    private void OnMessage(Entity<CitiNetMapCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not CitiNetUiMessageEvent)
            return;

        UpdateUI(ent, GetEntity(args.LoaderUid));
    }

    private void UpdateUI(EntityUid uid, EntityUid loader)
    {
        var sectors = new List<CitiNetMapSectorData>();
        var beacons = new List<CitiNetMapBeaconData>();
        var pings = new List<CitiNetMapPingData>();

        // Get the grid we are currently on
        var xform = Transform(loader);
        var gridUid = xform.GridUid;

        if (gridUid != null)
        {
            // 1. Scan for Sectors
            var sectorQuery = EntityQueryEnumerator<MapSectorComponent, TransformComponent>();
            while (sectorQuery.MoveNext(out var sUid, out var sector, out var sXform))
            {
                if (sXform.GridUid != gridUid)
                    continue;

                sectors.Add(new CitiNetMapSectorData(
                    sector.SectorName,
                    sector.Color,
                    sector.Bounds
                ));
            }

            // 2. Scan for Beacons (POIs)
            var beaconQuery = EntityQueryEnumerator<MapBeaconComponent, TransformComponent>();
            while (beaconQuery.MoveNext(out var bUid, out var beacon, out var bXform))
            {
                if (bXform.GridUid != gridUid || !beacon.IsVisible)
                    continue;

                beacons.Add(new CitiNetMapBeaconData(
                    GetNetEntity(bUid),
                    beacon.Label,
                    beacon.Icon,
                    beacon.Color,
                    bXform.LocalPosition
                ));
            }

            // 3. Scan for Dynamic Pings
            var pingQuery = EntityQueryEnumerator<CitiNetPingComponent, TransformComponent>();
            while (pingQuery.MoveNext(out var pUid, out var ping, out var pXform))
            {
                if (pXform.GridUid != gridUid)
                    continue;

                pings.Add(new CitiNetMapPingData(
                    pXform.LocalPosition,
                    ping.Color,
                    ping.Radius
                ));
            }
        }

        var state = new CitiNetMapBoundUserInterfaceState(gridUid != null ? GetNetEntity(gridUid.Value) : null, sectors, beacons, pings);
        _cartridge.UpdateCartridgeUiState(loader, state);
    }
}
