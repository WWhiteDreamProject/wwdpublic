using System.Linq;
using Content.Server.AlertLevel;
using Content.Server.Station.Systems;
using Content.Shared._White.Lockers;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;

namespace Content.Server._White.Lockers;


public sealed class StationAlertLevelLockSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly AlertLevelSystem _level = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAlertLevelLockComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<StationAlertLevelLockComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StationAlertLevelLockComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertChanged);
    }

    public void OnInit(EntityUid uid, StationAlertLevelLockComponent component, MapInitEvent args)
    {
        var station = _station.GetOwningStation(uid);

        if (station == null)
        {
            component.Enabled = false;
            Dirty(uid, component);
            return;
        }

        component.StationId = station.Value;

        CheckAlertLevels(component, _level.GetLevel(component.StationId.Value));
        Dirty(uid, component);
    }

    private void OnAlertChanged(AlertLevelChangedEvent args)
    {
        var enumerator = _entMan.AllEntityQueryEnumerator<StationAlertLevelLockComponent>();
        while (enumerator.MoveNext(out var uid, out var component))
        {
            var station = args.Station;

            if (station != component.StationId)
                continue;

            CheckAlertLevels(component, args.AlertLevel);
            Dirty(uid, component);
        }
    }

    private void CheckAlertLevels(StationAlertLevelLockComponent component, string newAlertLevel)
    {
        component.Locked = false;

        foreach (var level in component.LockedAlertLevels)
            if (level == newAlertLevel)
            {
                component.Locked = true;
                break;
            }
    }

    private void OnEmagged(EntityUid uid, StationAlertLevelLockComponent component, ref GotEmaggedEvent args)
    {
        args.Handled = true;
        component.Enabled = false;
        Dirty(uid, component);
    }

    public void OnExamined(EntityUid uid, StationAlertLevelLockComponent component, ExaminedEvent args)
    {
        if (!component.Enabled || component.LockedAlertLevels.Count == 0)
            return;

        var levels = string.Join(", ", component.LockedAlertLevels.Select( s => Loc.GetString($"alert-level-{s}").ToLower()));

        args.PushMarkup(Loc.GetString("station-alert-level-lock-examined", ("levels", levels)));
    }
}
