using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._White.Animations;
using Content.Shared._White.Animations.Prototypes;
using Content.Shared._White.Animations.Systems;
using Robust.Client.Animations;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Animations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;

namespace Content.Client._White.Animations.Systems;

public sealed class WhiteAnimationPlayerSystem : SharedWhiteAnimationPlayerSystem
{
    [Dependency] private readonly IComponentFactory _component = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private FrozenDictionary<string, AnimationPrototype> _animations = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<PlayAnimationMessage>(OnPlayAnimationMessage);
        SubscribeAllEvent<StopAnimationMessage>(OnStopAnimationMessage);

        _prototype.PrototypesReloaded += OnPrototypeReload;

        CachePrototypes();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _prototype.PrototypesReloaded -= OnPrototypeReload;
    }

    #region Event Handling

    private void OnPlayAnimationMessage(PlayAnimationMessage message) =>
        Play(GetEntity(message.AnimatedEntity), message.AnimationId, message.Force);

    private void OnStopAnimationMessage(StopAnimationMessage message) =>
        Stop(GetEntity(message.AnimatedEntity), message.AnimationKey);

    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<AnimationPrototype>())
            CachePrototypes();
    }

    private void CachePrototypes()
    {
        var animationPrototypes = _prototype.EnumeratePrototypes<AnimationPrototype>().ToList();

        foreach (var animationPrototype in animationPrototypes)
        {
            var animation = new Animation();
            var realLength = 0f;

            foreach (var animationTrackData in animationPrototype.AnimationTracksData)
            {
                AnimationTrack? animationTrack = null;
                if (animationTrackData is AnimationTrackComponentPropertyData componentPropertyData)
                    animationTrack = GetComponentProperty(componentPropertyData);
                else if (animationTrackData is AnimationTrackPlaySoundData playSoundData)
                    animationTrack = GetPlaySound(playSoundData);
                else if (animationTrackData is AnimationTrackSpriteFlickData spriteFlickData)
                    animationTrack = GetSpriteFlick(spriteFlickData);

                if (animationTrack == null)
                    continue;

                var animationTrackLength = 0f;
                foreach (var keyFrame in animationTrackData.KeyFrames)
                    animationTrackLength += keyFrame.Keyframe;

                realLength = MathF.Max(realLength, animationTrackLength);
                animation.AnimationTracks.Add(animationTrack);
            }

            animation.Length = animationPrototype.Length ?? TimeSpan.FromSeconds(realLength);

            animationPrototype.Animation = animation;
        }

        _animations = animationPrototypes.ToFrozenDictionary(x => x.ID);
    }
    #endregion

    #region Private API

    private AnimationTrackComponentProperty? GetComponentProperty(AnimationTrackComponentPropertyData animationTrackData)
    {
        var animationTrack = new AnimationTrackComponentProperty();

        if (!_component.TryGetRegistration(animationTrackData.ComponentType, out var registration, true))
            return null;

        animationTrack.ComponentType = registration.Type;
        animationTrack.Property = animationTrackData.Property;
        animationTrack.InterpolationMode = animationTrackData.InterpolationMode;

        var propertyType = AnimationHelper.GetAnimatableProperty(_component.GetComponent(registration), animationTrack.Property)?.GetType();
        if (propertyType == null)
            return null;

        foreach (var keyFrame in animationTrackData.KeyFrames)
        {
            if (keyFrame is not KeyFramePropertyData keyFrameProperty)
                continue;

            var value = _serialization.Read(propertyType, new ValueDataNode(keyFrameProperty.Value));
            if (value == null)
                continue;

            animationTrack.KeyFrames.Add(new (value, keyFrame.Keyframe));
        }

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
        var animationTrack = new AnimationTrackSpriteFlick {LayerKey = animationTrackData.LayerKey, };
        foreach (var keyFrame in animationTrackData.KeyFrames)
        {
            if (keyFrame is not KeyFrameSpriteFlickData keyFrameSpriteFlick)
                continue;

            animationTrack.KeyFrames.Add(new(keyFrameSpriteFlick.State, keyFrame.Keyframe));
        }

        return animationTrack;
    }
    #endregion

    #region Public API

    public bool TryGetAnimation(string animationId, [NotNullWhen(true)] out AnimationPrototype? animation) =>
        _animations.TryGetValue(animationId, out animation);

    public override void Play(EntityUid uid, ProtoId<AnimationPrototype> animationId, bool force = false)
    {
        if (!TryGetAnimation(animationId, out var animation) || !uid.Valid)
            return;

        if (_animationPlayer.HasRunningAnimation(uid, animation.Key))
        {
            if (!force)
                return;

            _animationPlayer.Stop(uid, animation.Key);
        }

        _animationPlayer.Play(uid, (Animation) animation.Animation, animation.Key);
    }

    public override void Play(EntityUid uid, ProtoId<AnimationPrototype> animationId, EntityUid recipient, bool force = false)
    {
        if (_player.LocalEntity != recipient)
            return;

        Play(uid, animationId, force);
    }

    public override void Play(EntityUid uid, ProtoId<AnimationPrototype> animationId, ICommonSession recipient, bool force = false)
    {
        if (_player.LocalSession != recipient)
            return;

        Play(uid, animationId, force);
    }

    public override void Play(EntityUid uid, ProtoId<AnimationPrototype> animationId, Filter filter, bool force = false)
    {
        if (!filter.Recipients.Contains(_player.LocalSession))
            return;

        Play(uid, animationId, force);
    }

    public override void PlayClient(EntityUid uid, ProtoId<AnimationPrototype> animationId, bool force = false) =>
        Play(uid, animationId, force);

    public override void PlayClient(EntityUid uid, ProtoId<AnimationPrototype> animationId, EntityUid recipient, bool force = false)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        Play(uid, animationId, recipient, force);
    }

    public override void PlayPredicted(EntityUid uid, ProtoId<AnimationPrototype> animationId, EntityUid recipient, bool force = false)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        Play(uid, animationId, recipient, force);
    }

    public override void Stop(EntityUid uid, string animationkey) =>
        _animationPlayer.Stop(uid, animationkey);

    public override void Stop(EntityUid uid, string animationkey, EntityUid recipient)
    {
        if (_player.LocalEntity != recipient)
            return;

        Stop(uid, animationkey);
    }

    public override void Stop(EntityUid uid, string animationkey, ICommonSession recipient)
    {
        if (_player.LocalSession != recipient)
            return;

        Stop(uid, animationkey);
    }

    public override void Stop(EntityUid uid, string animationkey, Filter filter)
    {
        if (!filter.Recipients.Contains(_player.LocalSession))
            return;

        Stop(uid, animationkey);
    }

    public override void StopClient(EntityUid uid, string animationkey, EntityUid recipient)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        Stop(uid, animationkey, recipient);
    }

    public override void StopPredicted(EntityUid uid, string animationkey, EntityUid recipient)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        Stop(uid, animationkey, recipient);
    }

    #endregion
}
