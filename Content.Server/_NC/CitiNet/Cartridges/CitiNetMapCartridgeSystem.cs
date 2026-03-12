using Content.Server.CartridgeLoader;
using Content.Shared._NC.CitiNet;
using Content.Shared.CartridgeLoader;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Content.Server._NC.CitiNet.Cartridges;

public sealed class CitiNetMapCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly CitiNetMapSystem _citiNet = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    private float _updateTimer = 0f;
    private const float UpdateInterval = 2.0f; 

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CitiNetMapCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<CitiNetMapCartridgeComponent, CartridgeMessageEvent>(OnMessage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _updateTimer += frameTime;
        if (_updateTimer >= UpdateInterval)
        {
            _updateTimer = 0f;
            RefreshAllInterfaces();
        }
    }

    private void RefreshAllInterfaces()
    {
        var query = EntityQueryEnumerator<CitiNetMapCartridgeComponent, CartridgeLoaderComponent>();
        while (query.MoveNext(out var uid, out var cart, out var loader))
        {
            UpdateUI(uid, loader.Owner);
        }
        
        var consoleQuery = EntityQueryEnumerator<CitiNetMapComponent, UserInterfaceComponent>();
        while (consoleQuery.MoveNext(out var uid, out var config, out var ui))
        {
            UpdateUI(uid, uid);
        }
    }

    private void OnUiReady(Entity<CitiNetMapCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUI(ent, args.Loader);
    }

    private void OnMessage(Entity<CitiNetMapCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not CitiNetUiMessageEvent) return;
        UpdateUI(ent, GetEntity(args.LoaderUid));
    }

    private void UpdateUI(EntityUid uid, EntityUid loader)
    {
        var sectors = new List<CitiNetMapSectorData>();
        var beacons = new List<CitiNetMapBeaconData>();
        var pings = new List<CitiNetMapPingData>();

        // 1. Identify the viewer (person looking at the map)
        EntityUid? viewer = null;
        foreach (var actor in _uiSystem.GetActors(loader, CitiNetMapUiKey.Key))
        {
            viewer = actor;
            break;
        }

        // Fallback: If no actors found via UI, check if the PDA is in someone's inventory/hands
        if (viewer == null && TryComp<TransformComponent>(loader, out var xformLoader))
        {
            viewer = xformLoader.ParentUid;
        }

        string? viewerJob = null;
        if (viewer != null && _mind.TryGetMind(viewer.Value, out var mindId, out var mind))
        {
            if (_job.MindTryGetJobId(mindId, out var jobId))
            {
                viewerJob = jobId?.Id;
            }
        }

        List<string> allowedGroups = new() { "Public" };
        if (TryComp<CitiNetMapComponent>(loader, out var mapConfig))
        {
            allowedGroups = mapConfig.VisibleGroups;
        }

        var xform = EntityManager.GetComponent<TransformComponent>(loader);
        var gridUid = xform.GridUid;

        if (gridUid != null)
        {
            // 2. Add SELF (Viewer) manually if they are on the grid
            if (viewer != null && TryComp<TransformComponent>(viewer.Value, out var viewerXform) && viewerXform.GridUid == gridUid)
            {
                beacons.Add(new CitiNetMapBeaconData(
                    GetNetEntity(viewer.Value),
                    "YOU",
                    null,
                    Color.FromHex("#00f2ff"),
                    viewerXform.LocalPosition,
                    12,
                    false,
                    true
                ));
            }

            // 3. Scan for Sectors
            var sectorQuery = EntityQueryEnumerator<MapSectorComponent, TransformComponent>();
            while (sectorQuery.MoveNext(out var sUid, out var sector, out var sXform))
            {
                if (sXform.GridUid != gridUid) continue;
                sectors.Add(new CitiNetMapSectorData(sector.SectorName, sector.Color, sector.Bounds, sector.FontSize));
            }

            // 4. Scan for Beacons
            var beaconQuery = EntityQueryEnumerator<MapBeaconComponent, TransformComponent>();
            while (beaconQuery.MoveNext(out var bUid, out var beacon, out var bXform))
            {
                if (bXform.GridUid != gridUid || !beacon.IsVisible) continue;

                // Skip if this is the viewer (we already added them as 'YOU')
                if (bUid == viewer) continue;
                
                if (!allowedGroups.Contains(beacon.Group))
                    continue;

                bool isDead = false;
                if (TryComp<MobStateComponent>(bUid, out var mobState))
                {
                    isDead = _mobState.IsDead(bUid, mobState);
                }

                var label = string.IsNullOrWhiteSpace(beacon.Label) ? MetaData(bUid).EntityName : beacon.Label;

                beacons.Add(new CitiNetMapBeaconData(
                    GetNetEntity(bUid),
                    label,
                    beacon.Icon,
                    beacon.Color,
                    bXform.LocalPosition,
                    beacon.FontSize,
                    isDead,
                    false
                ));
            }

            pings.AddRange(_citiNet.GetActivePings(gridUid.Value));
        }

        var state = new CitiNetMapBoundUserInterfaceState(gridUid != null ? GetNetEntity(gridUid.Value) : null, sectors, beacons, pings);
        
        if (HasComp<CitiNetMapCartridgeComponent>(uid))
            _cartridge.UpdateCartridgeUiState(loader, state);
        else
            _uiSystem.SetUiState(loader, CitiNetMapUiKey.Key, state);
    }
}
