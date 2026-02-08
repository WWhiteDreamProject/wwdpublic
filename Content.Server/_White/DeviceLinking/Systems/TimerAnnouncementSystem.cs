using Content.Server.Chat.Managers;
using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Events;
using Content.Shared.Chat;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.DeviceLinking.Systems
{
    public sealed class TimerAnnouncementSystem : EntitySystem
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<TimerAnnouncementComponent, SignalTimerStartedEvent>(OnTimerStarted);
            SubscribeLocalEvent<TimerAnnouncementComponent, SignalTimerTriggeredEvent>(OnTimerTriggered);
            SubscribeLocalEvent<TimerAnnouncementComponent, SignalTimerCancelledEvent>(OnTimerCancelled);
        }

        private void OnTimerStarted(EntityUid uid, TimerAnnouncementComponent comp, SignalTimerStartedEvent args)
        {
            if (string.IsNullOrEmpty(comp.StartMessage))
                return;

            SendChatMessage(uid, comp, comp.StartMessage);
        }

        private void OnTimerTriggered(EntityUid uid, TimerAnnouncementComponent comp, SignalTimerTriggeredEvent args)
        {
            if (string.IsNullOrEmpty(comp.EndMessage))
                return;

            SendChatMessage(uid, comp, comp.EndMessage);
        }

        private void OnTimerCancelled(EntityUid uid, TimerAnnouncementComponent comp, SignalTimerCancelledEvent args)
        {
            if (string.IsNullOrEmpty(comp.CancelMessage))
                return;

            SendChatMessage(uid, comp, comp.CancelMessage);
        }

        private void SendChatMessage(EntityUid uid, TimerAnnouncementComponent comp, string message)
        {
            // Add null checks for dependencies
            if (_chatManager == null)
                return;

            var senderFormatted = comp.SenderFont != null
                ? $"[font=\"{comp.SenderFont}\" size={comp.SenderFontSize}][color={comp.SenderColor}]{comp.Sender}:[/color][/font]"
                : $"[font size={comp.SenderFontSize}][color={comp.SenderColor}]{comp.Sender}:[/color][/font]";

            var messageFormatted = comp.MessageFont != null
                ? $"[font=\"{comp.MessageFont}\" size={comp.FontSize}][color={comp.TextColor}]{message}[/color][/font]"
                : $"[font size={comp.FontSize}][color={comp.TextColor}]{message}[/color][/font]";

            var wrappedMessage = $"{senderFormatted} {messageFormatted}";

            _chatManager.ChatMessageToAll(
                ChatChannel.Server,
                messageFormatted,
                wrappedMessage,
                uid,
                false,
                true
            );

            // Add null check for audio system
            if (comp.Sound != null && _audioSystem != null)
            {
                _audioSystem.PlayGlobal(comp.Sound, Filter.Broadcast(), true);
            }
        }
    }
}
