using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server._White.Chat.Systems;

public sealed class AnnouncementSpokeEvent : EntityEventArgs
{
    public readonly Filter Source;
    public readonly string AnnouncementSound;
    public readonly AudioParams AnnouncementSoundParams;
    public readonly string Message;

    public AnnouncementSpokeEvent(Filter source, string announcementSound, AudioParams announcementSoundParams, string message)
    {
        Source = source;
        Message = message;
        AnnouncementSound = announcementSound;
        AnnouncementSoundParams = announcementSoundParams;
    }
}

public sealed class RadioSpokeEvent : EntityEventArgs
{
    public readonly EntityUid Source;
    public readonly string Message;


    public RadioSpokeEvent(EntityUid source, string message)
    {
        Source = source;
        Message = message;
    }
}
