using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._White.CritDeathSounds;

public sealed class
    OnDeath : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CritDeathSoundsComponent, MobStateChangedEvent>(HandleDeathEvent);
        SubscribeLocalEvent<CritDeathSoundsComponent, PlayerDetachedEvent>(OnDetach);
    }

    private readonly Dictionary<EntityUid, EntityUid> _playingStreams = new();


    private void HandleDeathEvent(EntityUid uid, CritDeathSoundsComponent component, MobStateChangedEvent args)
    {
        switch (args.NewMobState)
        {
            case MobState.Invalid:
                StopPlayingStream(uid);
                break;
            case MobState.Alive:
                StopPlayingStream(uid);
                break;
            case MobState.Critical:
                PlayPlayingStream(uid, component);
                break;
            case MobState.Dead:
                StopPlayingStream(uid);
                PlayDeathSound(uid, component);
                break;
        }
    }

    private void PlayPlayingStream(EntityUid uid, CritDeathSoundsComponent component)
    {
        if (_playingStreams.TryGetValue(uid, out var currentStream))
        {
            _audio.Stop(currentStream);
        }

        var newStream = _audio.PlayEntity(component.HeartSounds, uid, uid, AudioParams.Default.WithLoop(true));

        if (newStream.HasValue)
        {
            _playingStreams[uid] = newStream.Value.Entity;
        }
    }

    private void StopPlayingStream(EntityUid uid)
    {
        if (!_playingStreams.TryGetValue(uid, out var currentStream))
            return;

        _audio.Stop(currentStream);
        _playingStreams.Remove(uid);
    }

    private void PlayDeathSound(EntityUid uid, CritDeathSoundsComponent component)
    {
        if (component.CanOtherHearDeathSound)
            _audio.PlayPvs(component.DeathSounds, uid, AudioParams.Default);
        else
            _audio.PlayEntity(component.DeathSounds, uid, uid, AudioParams.Default);
    }

    private void OnDetach(EntityUid uid, CritDeathSoundsComponent component, PlayerDetachedEvent args)
    {
        StopPlayingStream(args.Entity);
    }
}
