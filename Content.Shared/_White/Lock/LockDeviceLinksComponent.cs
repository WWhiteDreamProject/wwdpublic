using Content.Shared.DeviceLinking.Events;
using Content.Shared.Lock;

namespace Content.Shared._White.Lock;

[RegisterComponent]
public sealed partial class LockDeviceLinksComponent : Component;

public sealed class LockDeviceLinksSystem : EntitySystem
{
    [Dependency] private readonly LockSystem _lock = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LockDeviceLinksComponent, LinkAttemptEvent>(OnLinkAttempt);
    }

    private void OnLinkAttempt(EntityUid uid, LockDeviceLinksComponent comp, LinkAttemptEvent args)
    {
        if (_lock.IsLocked(uid))
            args.Cancel();
    }
}
