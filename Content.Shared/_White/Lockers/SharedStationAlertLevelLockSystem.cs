using Content.Shared.Lock;
using Content.Shared.Popups;

namespace Content.Shared._White.Lockers;

public sealed class SharedStationAlertLevelLockSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAlertLevelLockComponent, LockToggleAttemptEvent>(OnTryAccess);
    }

    private void OnTryAccess(Entity<StationAlertLevelLockComponent> ent, ref LockToggleAttemptEvent args)
    {
        if (!TryComp<LockComponent>(ent.Owner, out var lockComponent))
            return;
        var locking = !lockComponent.Locked; // Allow locking

        if (!ent.Comp.Enabled || !ent.Comp.Locked || locking)
            return;

        _popup.PopupClient(Loc.GetString("access-failed-wrong-station-alert-level"), ent.Owner, args.User);

        args.Cancelled = true;
    }
}
