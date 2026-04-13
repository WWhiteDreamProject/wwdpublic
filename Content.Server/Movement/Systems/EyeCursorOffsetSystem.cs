using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server.Movement.Systems;

public sealed class EyeCursorOffsetSystem : SharedEyeCursorOffsetSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RequestEyeCursorOffsetEvent>(OnRequestOffset);
    }

    protected override bool IsLocalPlayer(EntityUid uid) => false; // На сервере локальных игроков нет

    private void OnRequestOffset(RequestEyeCursorOffsetEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        if (!TryComp<EyeCursorOffsetComponent>(player, out var comp))
            return;

        // Валидация
        var offset = ev.Offset;
        if (offset.Length() > comp.MaxOffset + 1f)
            offset = offset.Normalized() * comp.MaxOffset;

        comp.CurrentOffset = offset;
        // Dirty не нужен для владельца (он сам шлет нам данные), 
        // но может быть полезен для других зрителей, если мы захотим синхронизировать их.
        Dirty(player, comp);
    }
}
