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

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<NcpdTabletComponent, BoundUIOpenedEvent>(OnTabletOpened);
            SubscribeLocalEvent<NcpdTabletComponent, NcpdTabletSelectCallMsg>(OnSelectCall);
            SubscribeLocalEvent<NcpdTabletComponent, NcpdTabletClearCallMsg>(OnClearCall);
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

        public void AddCall(string title, string sector, string description, NetCoordinates coordinates)
        {
            var call = new NcpdCallData(
                _nextCallId++,
                title,
                sector,
                description,
                coordinates,
                _timing.CurTime
            );

            _activeCalls.Add(call);
            if (_activeCalls.Count > 20)
                _activeCalls.RemoveAt(0);

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
                if (!string.IsNullOrEmpty(bComp.RequiredRole) && bComp.RequiredRole != "NCPD") continue;

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
            var ticket = EntityManager.SpawnEntity("DispatchCallTicket", Transform(consoleUid).Coordinates);
            if (TryComp<PaperComponent>(ticket, out var paper))
            {
                paper.Content = $"NCPD DISPATCH TICKET\n" +
                                $"-------------------\n" +
                                $"CASE ID: #{call.Id}\n" +
                                $"TYPE: {call.Title}\n" +
                                $"SECTOR: {call.Sector}\n" +
                                $"TIME: {call.CreatedTime.ToString(@"hh\:mm\:ss")}\n" +
                                $"DETAILS: {call.Description}\n" +
                                $"-------------------\n" +
                                $"SIGNED: DISPATCHER";
                Dirty(ticket, paper);
            }
        }
    }
}

