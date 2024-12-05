using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.TTS;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client._White.TTS;

// ReSharper disable once InconsistentNaming
public sealed class AnnounceTTSSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private ISawmill _sawmill = default!;
    private readonly MemoryContentRoot _contentRoot = new();
    private ResPath _prefix;

    private float _volume = 0.0f;
    private ulong _fileIdx = 0;
    private static ulong _shareIdx = 0;

    private TTSAudioStream? _currentlyPlaying;
    private readonly Queue<TTSAudioStream> _queuedStreams = new();

    /// <inheritdoc />
    public override void Initialize()
    {
        _prefix = ResPath.Root / $"TTSAnon{_shareIdx++}";
        _resourceCache.AddRoot(_prefix, _contentRoot);
        _sawmill = Logger.GetSawmill("AnnounceTTSSystem");
        _cfg.OnValueChanged(CCVars.AnnouncerVolume, OnTtsVolumeChanged, true);
        SubscribeNetworkEvent<AnnounceTTSEvent>(OnAnnounceTTSPlay);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnCleanup);
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        EndStreams();
        _contentRoot.Clear();
    }

    /// <inheritdoc />
    public override void FrameUpdate(float frameTime)
    {
        if (_queuedStreams.Count == 0)
            return;

        var isDoNext = true;
        try
        {
            isDoNext = _currentlyPlaying == null ||
                       (_currentlyPlaying.AudioStream != null && TerminatingOrDeleted(_currentlyPlaying.AudioStream!.Value))
                       || !(_currentlyPlaying.AudioStream?.Comp.Playing ?? false);
        }
        catch (Exception err)
        {
            isDoNext = true;
        }

        if (isDoNext)
        {
            _currentlyPlaying?.StopAndClean(this);
            ProcessEntityQueue();
        }

    }

    /// <inheritdoc />
    public override void Shutdown()
    {
        _cfg.UnsubValueChanged(CCVars.AnnouncerVolume, OnTtsVolumeChanged);
        EndStreams();
        _contentRoot.Dispose();
    }

    private void OnAnnounceTTSPlay(AnnounceTTSEvent ev)
    {
        var volume = Math.Max(-5f, SharedAudioSystem.GainToVolume(_volume));


        var file = new ResPath(ev.AnnouncementSound);

        if (!_resourceCache.TryGetResource<AudioResource>(file, out var audio))
        {
            _sawmill.Error($"Server tried to play audio file {ev.AnnouncementSound} which does not exist.");
            return;
        }

        if (TryCreateAudioSource(file, ev.AnnouncementParams.Volume, out var sourceAnnounce))
            AddEntityStreamToQueue(sourceAnnounce);
        if (ev.Data.Length > 0 && TryCreateAudioSource(ev.Data, volume, out var source))
        {
            source.DelayMs = (int) audio.AudioStream.Length.TotalMilliseconds;
            AddEntityStreamToQueue(source);
        }

    }

    private void AddEntityStreamToQueue(TTSAudioStream stream)
    {
        _queuedStreams.Enqueue(stream);
    }

    private void ProcessEntityQueue()
    {
        if (_queuedStreams.TryDequeue(out _currentlyPlaying))
            PlayEntity(_currentlyPlaying);
    }

    private bool TryCreateAudioSource(byte[] data, float volume, [NotNullWhen(true)] out TTSAudioStream? source)
    {
        var filePath = new ResPath($"{_fileIdx++}.ogg");
        _contentRoot.AddOrUpdateFile(filePath, data);

        var audioParams = AudioParams.Default.WithVolume(volume).WithRolloffFactor(1f).WithMaxDistance(float.MaxValue).WithReferenceDistance(1f);
        var soundPath = new SoundPathSpecifier(_prefix / filePath, audioParams);

        source = new TTSAudioStream(soundPath, filePath);

        return true;
    }

    private bool TryCreateAudioSource(ResPath audio, float volume,
        [NotNullWhen(true)] out TTSAudioStream? source)
    {
        var audioParams = AudioParams.Default.WithVolume(volume).WithRolloffFactor(1f).WithMaxDistance(float.MaxValue).WithReferenceDistance(1f);

        var soundPath = new SoundPathSpecifier(audio, audioParams);


        source = new TTSAudioStream(soundPath, null);

        return true;
    }

    private void PlayEntity(TTSAudioStream stream)
    {
        stream.AudioStream = _audio.PlayGlobal(stream.Source, Filter.Local(), false);
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void EndStreams()
    {
        foreach (var stream in _queuedStreams)
        {
            stream.StopAndClean(this);
        }

        _queuedStreams.Clear();
    }

    // ReSharper disable once InconsistentNaming
    private sealed class TTSAudioStream
    {
        public SoundPathSpecifier Source { get; }
        public ResPath? CacheFile { get; }
        public Entity<AudioComponent>? AudioStream { get; set; }

        public int DelayMs { get; set; }

        public TTSAudioStream(SoundPathSpecifier source, ResPath? cacheFile, int delayMs = 0)
        {
            Source = source;
            CacheFile = cacheFile;
            DelayMs = delayMs;
        }

        public void StopAndClean(AnnounceTTSSystem sys)
        {
            if (AudioStream != null)
            {
                sys._audio.Stop(AudioStream.Value,AudioStream.Value);

            }
            if (CacheFile != null)
            {
                sys._contentRoot.RemoveFile(CacheFile.Value);
            }
        }
    }
}
