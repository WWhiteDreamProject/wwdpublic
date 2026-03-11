using System;
using System.Collections.Generic;
using Content.Server.Station.Systems;
using Content.Shared._NC.Forensics;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Content.Server._NC.Forensics;
using Robust.Shared.Timing;
using Robust.Shared.Player;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Map;
using Robust.Shared.GameStates;

namespace Content.Server._NC.Forensics;

public sealed class NcpdForensicsSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<NcpdForensicsConsoleComponent, BoundUIOpenedEvent>(OnConsoleOpened);
    }

    private void OnMobStateChanged(EntityUid uid, MobStateComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
            return;

        // Find owning station
        var stationUid = _stationSystem.GetOwningStation(uid);
        if (stationUid == null)
            return;

        // Require ID card present
        if (!_idCardSystem.TryFindIdCard(uid, out var idCard))
            return;

        var coords = _transformSystem.GetMapCoordinates(uid);
        if (!_mapManager.TryGetMap(coords.MapId, out var map))
            return;

        var locString = $"{map.MapName ?? "Map"}";
        var pos = coords.Position;

        var alert = new ForensicsAlertData
        {
            Victim = idCard.Comp.FullName ?? MetaData(uid).EntityName,
            Location = locString,
            X = (float)Math.Round(pos.X, 1),
            Y = (float)Math.Round(pos.Y, 1),
            Time = _timing.CurTime.TimeOfDay
        };

        var station = EnsureComp<NcpdForensicsStationComponent>(stationUid.Value);
        station.Alerts.Insert(0, alert);
        if (station.Alerts.Count > 50)
            station.Alerts.RemoveAt(station.Alerts.Count - 1);
    }

    private void OnConsoleOpened(EntityUid uid, NcpdForensicsConsoleComponent component, BoundUIOpenedEvent args)
    {
        var stationUid = _stationSystem.GetOwningStation(uid);
        if (stationUid == null)
            return;

        var alerts = EnsureComp<NcpdForensicsStationComponent>(stationUid.Value).Alerts;
        var state = new NcpdForensicsConsoleBuiState(new List<ForensicsAlertData>(alerts));
        var ui = Comp<UserInterfaceComponent>(uid);
        ui.GetUiOrNull(NcpdForensicsConsoleUiKey.Key)?.SetState(state);
    }
}
