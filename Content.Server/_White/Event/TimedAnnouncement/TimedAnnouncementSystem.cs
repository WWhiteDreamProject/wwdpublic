using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared.Announcements.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.Chat;
using Robust.Server.Audio;

namespace Content.Server._White.Event.TimedAnnouncement;

public sealed class TimedAnnouncementSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TimedAnnouncementComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, TimedAnnouncementComponent comp, MapInitEvent args)
    {
        if (comp.Announcements.Count == 0)
            return;

        var cycleDuration = comp.Cycle > 0 ? comp.Cycle : comp.Announcements.Max(a => a.Delay);

        var repeatCount = comp.Repeat > 0 ? comp.Repeat : 1;
        for (int cycle = 0; cycle < repeatCount; cycle++)
        {
            foreach (var announcement in comp.Announcements)
            {
                var delay = TimeSpan.FromSeconds(announcement.Delay + (cycle * cycleDuration));
                Timer.Spawn(delay, () => SendAnnouncement(uid, comp, announcement));
            }
        }
    }

    private void SendAnnouncement(EntityUid uid, TimedAnnouncementComponent comp, TimedAnnouncementData data)
    {
        if (TerminatingOrDeleted(uid))
            return;

        var sender = data.Sender ?? comp.Sender;
        var sound = data.Sound ?? comp.Sound;
        var color = data.MessageColor ?? comp.MessageColor;

        var message = Loc.GetString(data.Announcement);
        var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message",
            ("sender", Loc.GetString(sender)),
            ("message", message));

        _chat.ChatMessageToAll(
            ChatChannel.Radio,
            message,
            wrappedMessage,
            default,
            false,
            true,
            color
        );

        _audio.PlayGlobal(
            sound == null
                ? new ResolvedPathSpecifier(ChatSystem.DefaultAnnouncementSound)
                : _audio.ResolveSound(sound),
            Filter.Broadcast(),
            true,
            AudioParams.Default.WithVolume(-2f)
        );
    }
}
