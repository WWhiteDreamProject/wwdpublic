using System;
using System.Collections.Generic;
using Content.Server.Station.Systems;
using Content.Shared._NC.Forensics;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Robust.Shared.Timing;
using Robust.Shared.Player;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Map;
using Robust.Shared.GameStates;
using Content.Server._NC.Ncpd;
using Content.Shared._NC.Ncpd;
using System.Linq;

namespace Content.Server._NC.Forensics;

public sealed class NcpdForensicsSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly NcpdDispatchSystem _dispatchSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<NcpdForensicsConsoleComponent, BoundUIOpenedEvent>(OnConsoleOpened);
        
        Subs.BuiEvents<NcpdForensicsConsoleComponent>(NcpdForensicsConsoleUiKey.Key, subs => {
            subs.Event<NcpdForensicsAlertActionMessage>(OnAlertAction);
        });
    }

    private void OnAlertAction(EntityUid uid, NcpdForensicsConsoleComponent component, NcpdForensicsAlertActionMessage msg)
    {
        var stationUid = _stationSystem.GetOwningStation(uid);
        if (stationUid == null && _stationSystem.GetStationsSet().Count > 0)
        {
            stationUid = _stationSystem.GetStationsSet().First();
        }

        if (stationUid == null)
            return;

        var station = EnsureComp<NcpdForensicsStationComponent>(stationUid.Value);
        if (msg.AlertIndex < 0 || msg.AlertIndex >= station.Alerts.Count)
            return;

        var alert = station.Alerts[msg.AlertIndex];

        switch (msg.Action)
        {
            case NcpdForensicsAlertAction.DispatchToTablet:
                if (alert.Dispatched)
                    break;

                var mapCoords = new MapCoordinates(alert.X, alert.Y, _transformSystem.GetMapCoordinates(uid).MapId);
                var netCoords = GetNetCoordinates(_transformSystem.ToCoordinates(mapCoords));
                _dispatchSystem.AddCall(Loc.GetString("nspd-call-type-flatline"), alert.Location, Loc.GetString("nspd-call-desc-victim", ("name", alert.Victim)), netCoords, $"forensics_{msg.AlertIndex}");
                
                alert.Dispatched = true;
                station.Alerts[msg.AlertIndex] = alert; // Update the struct in list
                break;
            case NcpdForensicsAlertAction.PrintTicket:
                var call = new NcpdCallData(0, Loc.GetString("nspd-call-type-flatline"), alert.Location, Loc.GetString("nspd-call-desc-victim", ("name", alert.Victim)), default, alert.Time);
                _dispatchSystem.SpawnDispatchTicket(uid, call);
                break;
        }
        UpdateConsoleUi(uid); 
    }

    private void UpdateConsoleUi(EntityUid uid)
    {
        var stationUid = _stationSystem.GetOwningStation(uid);
        if (stationUid == null && _stationSystem.GetStationsSet().Count > 0)
        {
            stationUid = _stationSystem.GetStationsSet().First();
        }

        if (stationUid == null)
            return;

        var alerts = EnsureComp<NcpdForensicsStationComponent>(stationUid.Value).Alerts;
        var state = new NcpdForensicsConsoleBuiState(new List<ForensicsAlertData>(alerts));
        _uiSystem.SetUiState(uid, NcpdForensicsConsoleUiKey.Key, state);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
            return;

        var target = args.Target;

        // Find owning station
        var stationUid = _stationSystem.GetOwningStation(target);
        if (stationUid == null && _stationSystem.GetStationsSet().Count > 0)
        {
            stationUid = _stationSystem.GetStationsSet().First();
        }

        if (stationUid == null)
            return;

        // Require ID card present
        if (!_idCardSystem.TryFindIdCard(target, out var idCard))
            return;

        var coords = _transformSystem.GetMapCoordinates(target);
        var mapUid = _mapManager.GetMapEntityId(coords.MapId);
        
        var locString = Name(mapUid);
        var pos = coords.Position;

        var alert = new ForensicsAlertData
        {
            Victim = idCard.Comp.FullName ?? Name(target),
            Location = locString,
            X = (float)Math.Round(pos.X, 1),
            Y = (float)Math.Round(pos.Y, 1),
            Time = _timing.CurTime
        };

        var station = EnsureComp<NcpdForensicsStationComponent>(stationUid.Value);
        station.Alerts.Insert(0, alert);
        if (station.Alerts.Count > 50)
            station.Alerts.RemoveAt(station.Alerts.Count - 1);
    }

    private void OnConsoleOpened(EntityUid uid, NcpdForensicsConsoleComponent component, BoundUIOpenedEvent args)
    {
        UpdateConsoleUi(uid);
    }
}

