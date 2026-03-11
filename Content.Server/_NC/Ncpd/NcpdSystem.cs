using Content.Shared._NC.Ncpd;
using Content.Server.Station.Systems;
using Robust.Shared.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System.Collections.Generic;

namespace Content.Server._NC.Ncpd;

public sealed class NcpdSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NcpdStationComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, NcpdStationComponent component, ComponentInit args)
    {
        // Init logic if any
    }

    public void AddLog(EntityUid stationUid, string officerName, string targetName, int amount, string status, string reason)
    {
        var ncpd = EnsureComp<NcpdStationComponent>(stationUid);

        var log = new NcpdLogEntry
        {
            Time = _timing.CurTime,
            OfficerName = officerName,
            TargetName = targetName,
            Amount = amount,
            Status = status,
            Reason = reason
        };

        ncpd.Logs.Insert(0, log); // Add to the beginning

        // Keep only last 100 logs
        if (ncpd.Logs.Count > 100)
        {
            ncpd.Logs.RemoveAt(ncpd.Logs.Count - 1);
        }

        Dirty(stationUid, ncpd);
    }

    public bool IsSuspended(EntityUid stationUid, EntityUid officerUid)
    {
        if (!TryComp<NcpdStationComponent>(stationUid, out var ncpd))
            return false;

        return ncpd.SuspendedOfficers.Contains(officerUid);
    }

    public void SetSuspended(EntityUid stationUid, EntityUid officerUid, bool suspended)
    {
        var ncpd = EnsureComp<NcpdStationComponent>(stationUid);

        if (suspended)
            ncpd.SuspendedOfficers.Add(officerUid);
        else
            ncpd.SuspendedOfficers.Remove(officerUid);

        Dirty(stationUid, ncpd);
    }
}
