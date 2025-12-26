using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._White.MobThresholdSounds;

public sealed class MobThresholdSoundsSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobThresholdSoundsComponent, MobStateChangedEvent>(HandleStateChange);
        SubscribeLocalEvent<MobThresholdSoundsComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<MobThresholdSoundsComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void HandleStateChange(EntityUid uid, MobThresholdSoundsComponent component, MobStateChangedEvent args)
    {
        PlaySound(uid, component, args.NewMobState);
    }

    private void PlaySound(EntityUid uid, MobThresholdSoundsComponent component, MobState mobstate, bool playdeathsound = true)
    {
        switch (mobstate)
        {
            case MobState.Invalid:
                StopPlayingStream(component);
                break;
            case MobState.Alive:
                StopPlayingStream(component);
                break;
            case MobState.Critical:
                StartPlayingStream(uid, component);
                break;
            case MobState.Dead:
                StopPlayingStream(component);
                if (playdeathsound)
                    PlayDeathSound(uid, component);
                break;
        }
    }

    private void StartPlayingStream(EntityUid uid, MobThresholdSoundsComponent component)
    {
        if (Exists(component.AudioStream))
        {
            _audio.Stop(component.AudioStream);
        }

        var newStream = _audio.PlayEntity(component.HeartSounds, uid, uid, AudioParams.Default.WithLoop(true));

        if (newStream.HasValue)
        {
            component.AudioStream = newStream.Value.Entity;
        }
    }

    private void StopPlayingStream(MobThresholdSoundsComponent component)
    {
        if (!Exists(component.AudioStream))
            return;

        _audio.Stop(component.AudioStream);
        Del(component.AudioStream);
    }

    private void PlayDeathSound(EntityUid uid, MobThresholdSoundsComponent component)
    {
        if (component.CanOtherHearDeathSound)
            _audio.PlayPvs(component.DeathSounds, uid, AudioParams.Default);
        else
            _audio.PlayEntity(component.DeathSounds, uid, uid, AudioParams.Default);
    }

    private void OnPlayerAttached(EntityUid uid, MobThresholdSoundsComponent component, PlayerAttachedEvent args)
    {
        if (!TryComp<MobThresholdsComponent>(args.Entity, out var thresholdsComponent))
            return;

        PlaySound(uid, component, thresholdsComponent.CurrentThresholdState, false);
    }

    private void OnPlayerDetached(EntityUid uid, MobThresholdSoundsComponent component, PlayerDetachedEvent args)
    {
        StopPlayingStream(component);
    }
}
