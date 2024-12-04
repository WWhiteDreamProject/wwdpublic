using System.IO;
using Content.Shared._White;
using Content.Shared._White.TTS.Events;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Configuration;

namespace Content.Client._White.TTS;

// ReSharper disable InconsistentNaming
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IAudioManager _audioManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;

    private float _volume;
    private readonly Dictionary<EntityUid, AudioComponent> _currentlyPlaying = new();

    private readonly Dictionary<EntityUid, Queue<AudioStreamWithParams>> _enquedStreams = new();

    // Same as Server.ChatSystem.VoiceRange
    private const float VoiceRange = 10;

    public override void Initialize()
    {
        _cfg.OnValueChanged(WhiteCVars.TTSVolume, OnTtsVolumeChanged, true);

        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(WhiteCVars.TTSVolume, OnTtsVolumeChanged);
        ClearQueues();
    }

    public override void FrameUpdate(float frameTime)
    {
        foreach (var (uid, audioComponent) in _currentlyPlaying)
        {
            if (!Deleted(uid) && audioComponent is { Running: true, Playing: true }
                || !_enquedStreams.TryGetValue(uid, out var queue)
                || !queue.TryDequeue(out var toPlay))
                continue;

            var audio = _audioSystem.PlayEntity(toPlay.Stream, uid, toPlay.Params);
            if (!audio.HasValue)
                continue;

            _currentlyPlaying[uid] = audio.Value.Component;
        }
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        PlayTTS(GetEntity(ev.Uid), ev.Data, ev.BoostVolume ? _volume + 5 : _volume);
    }

    public void PlayTTS(EntityUid uid, byte[] data, float volume)
    {
        if (_volume <= -20f)
            return;

        var stream = CreateAudioStream(data);

        var audioParams = new AudioParams
        {
            Volume = volume,
            MaxDistance = VoiceRange
        };

        var audioStream = new AudioStreamWithParams(stream, audioParams);
        EnqueueAudio(uid, audioStream);
    }

    public void StopCurrentTTS(EntityUid uid)
    {
        if (!_currentlyPlaying.TryGetValue(uid, out var audio))
            return;

        _audioSystem.Stop(audio.Owner);
    }

    private void EnqueueAudio(EntityUid uid, AudioStreamWithParams audioStream)
    {
        if (!_currentlyPlaying.ContainsKey(uid))
        {
            var audio = _audioSystem.PlayEntity(audioStream.Stream, uid, audioStream.Params);
            if (!audio.HasValue)
                return;

            _currentlyPlaying[uid] = audio.Value.Component;
            return;
        }

        if (_enquedStreams.TryGetValue(uid, out var queue))
        {
            queue.Enqueue(audioStream);
            return;
        }

        queue = new Queue<AudioStreamWithParams>();
        queue.Enqueue(audioStream);
        _enquedStreams[uid] = queue;
    }

    private void ClearQueues()
    {
        foreach (var (_, queue) in _enquedStreams)
        {
            queue.Clear();
        }
    }

    private AudioStream CreateAudioStream(byte[] data)
    {
        var dataStream = new MemoryStream(data) { Position = 0 };
        return _audioManager.LoadAudioOggVorbis(dataStream);
    }

    private record AudioStreamWithParams(AudioStream Stream, AudioParams Params);
}
