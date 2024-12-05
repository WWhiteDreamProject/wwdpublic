using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._White.TTS;

public enum VoiceRequestType
{
    None,
    Preview
}

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class PlayTTSEvent(byte[] data, NetEntity? sourceUid = null, bool isWhisper = false) : EntityEventArgs
{
    public byte[] Data { get; } = data;
    public NetEntity? SourceUid { get; } = sourceUid;
    public bool IsWhisper { get; } = isWhisper;
}

// ReSharper disable once InconsistentNaming
[Serializable, NetSerializable]
public sealed class RequestGlobalTTSEvent(VoiceRequestType text, string voiceId) : EntityEventArgs
{
    public VoiceRequestType Text { get;} = text;
    public string VoiceId { get; } = voiceId;
}

// ReSharper disable once InconsistentNaming
[Serializable, NetSerializable]
public sealed class RequestPreviewTTSEvent(string voiceId) : EntityEventArgs
{
    public string VoiceId { get; } = voiceId;
}

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeVoiceMessage(string voice) : BoundUserInterfaceMessage
{
    public string Voice = voice;
}

// ReSharper disable once InconsistentNaming
[Serializable, NetSerializable]
public sealed class AnnounceTTSEvent(byte[] data, string announcementSound, AudioParams announcementParams) : EntityEventArgs
{
    public byte[] Data { get; } = data;
    public string AnnouncementSound { get; } = announcementSound;
    public AudioParams AnnouncementParams{ get; } = announcementParams;
}
