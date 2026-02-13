using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.DeviceLinking.Components;
using Content.Shared._White.Event.CapturePoint;
using Content.Shared.Chat;
using Content.Shared.TextScreen;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._White.Event.CapturePoint;

public sealed class CapturePointSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CapturePointComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<CapturePointComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnEntInserted(Entity<CapturePointComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.CartridgeSlot.ID)
            return;

        var activeSignalTimer = EnsureComp<ActiveSignalTimerComponent>(ent);
        activeSignalTimer.TriggerTime = _gameTiming.CurTime + ent.Comp.Delay;

        _appearance.SetData(ent, TextScreenVisuals.TargetTime, activeSignalTimer.TriggerTime);

        SendChatMessage(ent, ent.Comp.StartMessage);
    }

    private void OnEntRemoved(Entity<CapturePointComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.CartridgeSlot.ID || !HasComp<ActiveSignalTimerComponent>(ent))
            return;

        RemComp<ActiveSignalTimerComponent>(ent);

        _appearance.SetData(ent, TextScreenVisuals.TargetTime, _gameTiming.CurTime);

        if (!TryComp<CapturePointCartridgeComponent>(args.Entity, out var cartridge))
            return;

        SendChatMessage(ent, ent.Comp.CancelMessage, cartridge.TeamMessage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveSignalTimerComponent, CapturePointComponent>();
        while (query.MoveNext(out var uid, out var activeSignalTimer, out var capturePoint))
        {
            if (activeSignalTimer.TriggerTime > _gameTiming.CurTime)
                continue;

            RemComp<ActiveSignalTimerComponent>(uid);
            QueueDel(capturePoint.CartridgeSlot.Item);

            SendChatMessage((uid, capturePoint), capturePoint.EndMessage);
        }
    }

    private void SendChatMessage(Entity<CapturePointComponent> ent, LocId message)
    {
        if (!TryComp<CapturePointCartridgeComponent>(ent.Comp.CartridgeSlot.Item, out var cartridge))
            return;

        SendChatMessage(ent, message, cartridge.TeamMessage);
    }

    private void SendChatMessage(Entity<CapturePointComponent> ent, LocId messageLoc, LocId teamMessage)
    {
        var sender = Loc.GetString(ent.Comp.Sender);
        var message = Loc.GetString(messageLoc, ("team", Loc.GetString(teamMessage)));
        var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender), ("message", message));

        _chat.ChatMessageToAll(
            ChatChannel.Radio,
            message,
            wrappedMessage,
            default,
            false,
            true,
            ent.Comp.MessageColor
        );

        _audio.PlayGlobal(ent.Comp.Sound == null ? new ResolvedPathSpecifier(ChatSystem.DefaultAnnouncementSound) : _audio.ResolveSound(ent.Comp.Sound), Filter.Broadcast(), true, AudioParams.Default.WithVolume(-2f));
    }
}
