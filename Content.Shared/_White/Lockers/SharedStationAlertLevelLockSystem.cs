using Content.Shared.Popups;


namespace Content.Shared._White.Lockers;


public sealed class SharedStationAlertLevelLockSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAlertLevelLockComponent, PreventLockAccessEvent>(OnTryAccess);
    }

    public void OnTryAccess(EntityUid uid, StationAlertLevelLockComponent component, PreventLockAccessEvent args)
    {
        if (!component.Enabled || !component.Locked)
            return;

        _popup.PopupClient(Loc.GetString("access-failed-wrong-station-alert-level"), uid, args.User);

        args.Cancel();
    }
}
