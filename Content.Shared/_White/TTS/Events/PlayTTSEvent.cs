using Robust.Shared.Serialization;

namespace Content.Shared._White.TTS.Events;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class PlayTTSEvent(NetEntity uid, byte[] data, bool boostVolume) : EntityEventArgs
{
    public NetEntity Uid { get; } = uid;

    public byte[] Data { get; } = data;

    public bool BoostVolume { get; } = boostVolume;
}
