using Content.Shared._NC.CitiNet;
using Content.Shared._NC.Ncpd;
using Content.Shared.Paper;
using Content.Server._NC.CitiNet;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server._NC.Ncpd
{
    public sealed class NcpdDispatchSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly CitiNetMapSystem _citiNetMapSystem = default!;

        private readonly List<NcpdCallData> _activeCalls = new();
        private int _nextCallId = 1;
        private float _updateTimer = 0f;
        private const float UpdateInterval = 5.0f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<NcpdTabletComponent, BoundUIOpenedEvent>(OnTabletOpened);
            SubscribeLocalEvent<NcpdTabletComponent, NcpdTabletSelectCallMsg>(OnSelectCall);
            SubscribeLocalEvent<NcpdTabletComponent, NcpdTabletClearCallMsg>(OnClearCall);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _updateTimer += frameTime;
            if (_updateTimer >= UpdateInterval)
            {
                _updateTimer = 0f;
                UpdateAllTablets();
            }
        }

        private void OnTabletOpened(EntityUid uid, NcpdTabletComponent component, BoundUIOpenedEvent args)
        {
            UpdateTabletUi(uid, component);
        }

        private void OnSelectCall(EntityUid uid, NcpdTabletComponent component, NcpdTabletSelectCallMsg args)
        {
            component.ActiveCallId = args.CallId;
            UpdateTabletUi(uid, component);
        }

        private void OnClearCall(EntityUid uid, NcpdTabletComponent component, NcpdTabletClearCallMsg args)
        {
            if (component.ActiveCallId == args.CallId)
                component.ActiveCallId = null;
            
            UpdateTabletUi(uid, component);
        }

        public void AddCall(string title, string sector, string description, NetCoordinates coordinates, string sourceId = "")
        {
            // If already dispatched, ignore (safety check)
            if (!string.IsNullOrEmpty(sourceId) && _activeCalls.Any(c => c.SourceId == sourceId))
                return;

            var call = new NcpdCallData(
                _nextCallId++,
                title,
                sector,
                description,
                coordinates,
                _timing.CurTime,
                sourceId
            );

            _activeCalls.Add(call);
            if (_activeCalls.Count > 20)
                _activeCalls.RemoveAt(0);

            UpdateAllTablets();
        }

        public void RemoveCallBySource(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId))
                return;

            _activeCalls.RemoveAll(c => c.SourceId == sourceId);
            UpdateAllTablets();
        }

        public void UpdateAllTablets()
        {
            var query = EntityQueryEnumerator<NcpdTabletComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                UpdateTabletUi(uid, comp);
            }
        }

        private void UpdateTabletUi(EntityUid uid, NcpdTabletComponent component)
        {
            if (!_ui.IsUiOpen(uid, NcpdTabletUiKey.Key))
                return;

            var gridUid = _transform.GetGrid(uid);
            var mapUid = _transform.GetMap(uid);

            var sectors = new List<CitiNetMapSectorData>();
            var sectorQuery = EntityQueryEnumerator<MapSectorComponent>();
            while (sectorQuery.MoveNext(out var sUid, out var sComp))
            {
                sectors.Add(new CitiNetMapSectorData(sComp.SectorName, sComp.Color, sComp.Bounds, sComp.FontSize));
            }

            var beacons = new List<CitiNetMapBeaconData>();
            var beaconQuery = EntityQueryEnumerator<MapBeaconComponent, TransformComponent>();
            while (beaconQuery.MoveNext(out var bUid, out var bComp, out var bXform))
            {
                if (!bComp.IsVisible) continue;
                
                // SHOW ONLY PUBLIC BEACONS: No required role AND group is Public
                if (!string.IsNullOrEmpty(bComp.RequiredRole)) continue;
                if (bComp.Group != "Public") continue;

                var bPos = _transform.GetGridOrMapTilePosition(bUid, bXform);
                beacons.Add(new CitiNetMapBeaconData(
                    GetNetEntity(bUid),
                    bComp.Label,
                    bComp.Icon,
                    bComp.Color,
                    bPos,
                    bComp.FontSize
                ));
            }

            var pings = _citiNetMapSystem.GetActivePings(gridUid ?? mapUid ?? uid);

            _ui.SetUiState(uid, NcpdTabletUiKey.Key, new NcpdTabletState(
                _activeCalls, 
                component.ActiveCallId, 
                GetNetEntity(gridUid ?? mapUid ?? uid),
                sectors, 
                beacons, 
                pings));
        }

        public void SpawnDispatchTicket(EntityUid consoleUid, NcpdCallData call)
        {
            var ticket = EntityManager.SpawnEntity("Paper", Transform(consoleUid).Coordinates);
            if (TryComp<PaperComponent>(ticket, out var paper))
            {
                paper.Content = $"{Loc.GetString("nspd-dispatch-ticket-title")}\n" +
                                $"-------------------\n" +
                                $"{Loc.GetString("nspd-dispatch-ticket-case")}{call.Id}\n" +
                                $"{Loc.GetString("nspd-dispatch-ticket-type")}{call.Title}\n" +
                                $"{Loc.GetString("nspd-dispatch-ticket-sector")}{call.Sector}\n" +
                                $"{Loc.GetString("nspd-dispatch-ticket-time")}{call.CreatedTime.ToString(@"hh\:mm\:ss")}\n" +
                                $"{Loc.GetString("nspd-dispatch-ticket-details")}{call.Description}\n" +
                                $"-------------------\n" +
                                $"{Loc.GetString("nspd-dispatch-ticket-sign")}";
                Dirty(ticket, paper);
            }
        }
    }
}

