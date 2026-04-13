using Content.Shared.Camera;
using Content.Shared.Movement.Components;
using Robust.Shared.Network;

namespace Content.Shared.Movement.Systems;

public abstract class SharedEyeCursorOffsetSystem : EntitySystem
{
    [Dependency] protected readonly INetManager NetManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeCursorOffsetComponent, GetEyeOffsetEvent>(OnGetEyeOffset);
    }

    protected virtual void OnGetEyeOffset(EntityUid uid, EyeCursorOffsetComponent component, ref GetEyeOffsetEvent args)
    {
        // На клиенте для локального игрока мы используем отдельную логику в EyeCursorOffsetSystem (Client)
        // чтобы избежать сетевых рывков.
        if (NetManager.IsClient && IsLocalPlayer(uid))
            return;

        args.Offset += component.CurrentOffset;
    }

    protected abstract bool IsLocalPlayer(EntityUid uid);
}
