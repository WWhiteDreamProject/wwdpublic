using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._White.Animations;
using Content.Shared._White.Animations.Prototypes;
using Content.Shared._White.Animations.Systems;
using Robust.Client.Animations;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Animations.Systems;

public sealed class WhiteAnimationPlayerSystem : SharedWhiteAnimationPlayerSystem
{
    [Dependency] private readonly IComponentFactory _component = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private FrozenDictionary<string, AnimationPrototype> _animations = default!;

    public override void Initialize()
    {
        base.Initialize();

        _prototype.PrototypesReloaded += OnPrototypeReload;
        SubscribeNetworkEvent<PlayAnimationMessage>(OnPlayAnimationMessage);

        CachePrototypes();
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<AnimationPrototype>())
            CachePrototypes();
    }

    private void OnPlayAnimationMessage(PlayAnimationMessage message) =>
        Play(GetEntity(message.AnimatedEntity), message.AnimationId);

    private void CachePrototypes()
    {
        var animationPrototypes = _prototype.EnumeratePrototypes<AnimationPrototype>().ToList();

        foreach (var animationPrototype in animationPrototypes)
        {
            var animation = new Animation
            {
                Length = animationPrototype.Length
            };

            foreach (var animationTrackData in animationPrototype.AnimationTracksData)
            {
                AnimationTrack? animationTrack = null;
                if (animationTrackData is AnimationTrackComponentPropertyData componentPropertyData)
                    animationTrack = GetComponentProperty(componentPropertyData);
                else if (animationTrackData is AnimationTrackControlPropertyData controlPropertyData)
                    animationTrack = GetControlProperty(controlPropertyData);
                else if (animationTrackData is AnimationTrackPlaySoundData playSoundData)
                    animationTrack = GetPlaySound(playSoundData);
                else if (animationTrackData is AnimationTrackSpriteFlickData spriteFlickData)
                    animationTrack = GetSpriteFlick(spriteFlickData);

                if (animationTrack == null)
                    return;

                animation.AnimationTracks.Add(animationTrack);
            }

            animationPrototype.Animation = animation;
        }

        _animations = animationPrototypes.ToFrozenDictionary(x => x.ID);
    }

    private AnimationTrackComponentProperty GetComponentProperty(AnimationTrackComponentPropertyData animationTrackData)
    {
        var animationTrack = new AnimationTrackComponentProperty();

        if (!_component.TryGetRegistration(animationTrackData.ComponentType, out var registration, true))
            return animationTrack;

        animationTrack.ComponentType = registration.Type;
        animationTrack.Property = animationTrackData.Property;
        SetProperty(animationTrack, animationTrackData);

        return animationTrack;
    }

    private AnimationTrackControlProperty GetControlProperty(AnimationTrackControlPropertyData animationTrackData)
    {
        var animationTrack = new AnimationTrackControlProperty
        {
            Property = animationTrackData.Property
        };

        SetProperty(animationTrack, animationTrackData);

        return animationTrack;
    }

    private AnimationTrackPlaySound GetPlaySound(AnimationTrackPlaySoundData animationTrackData)
    {
        var animationTrack = new AnimationTrackPlaySound();
        foreach (var keyFrame in animationTrackData.KeyFrames)
        {
            if (keyFrame is not KeyFrameSoundData keyFrameSound)
                continue;

            animationTrack.KeyFrames.Add(new(_audio.ResolveSound(keyFrameSound.Sound), keyFrame.Keyframe));
        }

        return animationTrack;
    }

    private AnimationTrackSpriteFlick GetSpriteFlick(AnimationTrackSpriteFlickData animationTrackData)
    {
        var animationTrack = new AnimationTrackSpriteFlick();
        foreach (var keyFrame in animationTrackData.KeyFrames)
        {
            if (keyFrame is not KeyFrameSpriteFlickData keyFrameSpriteFlick)
                continue;

            animationTrack.KeyFrames.Add(new(keyFrameSpriteFlick.State, keyFrame.Keyframe));
        }

        return animationTrack;
    }

    private void SetProperty(AnimationTrackProperty animationTrack, AnimationTrackPropertyData animationTrackData)
    {
        animationTrack.InterpolationMode = animationTrackData.InterpolationMode;

        foreach (var keyFrame in animationTrackData.KeyFrames)
        {
            if (keyFrame is not KeyFramePropertyData keyFrameProperty)
                continue;

            animationTrack.KeyFrames.Add(new (keyFrameProperty.Value.Value, keyFrame.Keyframe));
        }
    }

    public bool TryGetAnimation(string animationId, [NotNullWhen(true)] out AnimationPrototype? animation) =>
        _animations.TryGetValue(animationId, out animation);

    public override void Play(EntityUid uid, ProtoId<AnimationPrototype> animationId)
    {
        if (!TryGetAnimation(animationId, out var animation)
            || !uid.Valid
            || _animationPlayer.HasRunningAnimation(uid, animation.Key))
            return;

        _animationPlayer.Play(uid, (Animation) animation.Animation, animation.Key);
    }
}
