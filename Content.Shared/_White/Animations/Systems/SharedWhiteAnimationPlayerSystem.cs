using Content.Shared._White.Animations.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Animations.Systems;

public abstract class SharedWhiteAnimationPlayerSystem : EntitySystem
{
    #region Public API

    /// <summary>
    /// Play animation on entity for every player in pvs range.
    /// </summary>
    /// <param name="uid">The UID of the entity.</param>
    /// <param name="animationId">The ID of the animation that will be played.</param>
    /// <param name="force">Determines whether this animation should play if an animation with the same key is already playing.</param>
    public abstract void Play(EntityUid uid, ProtoId<AnimationPrototype> animationId, bool force = false);

    /// <summary>
    /// Variant of <see cref="Play(EntityUid, ProtoId{AnimationPrototype}, bool)"/> that play animation only to some specific client.
    /// </summary>
    public abstract void Play(EntityUid uid, ProtoId<AnimationPrototype> animationId, EntityUid recipient, bool force = false);

    /// <summary>
    /// Variant of <see cref="Play(EntityUid, ProtoId{AnimationPrototype}, bool)"/> that play animation only to some specific client.
    /// </summary>
    public abstract void Play(EntityUid uid, ProtoId<AnimationPrototype> animationId, ICommonSession recipient, bool force = false);

    /// <summary>
    /// Filtered variant of <see cref="Play(EntityUid, ProtoId{AnimationPrototype}, bool)"/>, which should only be used
    /// if the filtering has to be more specific than simply pvs range based.
    /// </summary>
    public abstract void Play(EntityUid uid, ProtoId<AnimationPrototype> animationId, Filter filter, bool force = false);

    /// <summary>
    /// Variant of <see cref="Play(EntityUid, ProtoId{AnimationPrototype}, bool)"/> that only runs on the client, outside of prediction.
    /// Useful for shared code always run by both sides to avoid animation duplicate.
    /// </summary>
    public abstract void PlayClient(EntityUid uid, ProtoId<AnimationPrototype> animationId, bool force = false);

    /// <summary>
    /// Variant of <see cref="Play(EntityUid, ProtoId{AnimationPrototype}, EntityUid, bool)"/> that only runs on the client, outside of prediction.
    /// Useful for shared code always run by both sides to avoid animation duplicate.
    /// </summary>
    public abstract void PlayClient(EntityUid uid, ProtoId<AnimationPrototype> animationId, EntityUid recipient, bool force = false);

    /// <summary>
    /// Variant of <see cref="Play(EntityUid, ProtoId{AnimationPrototype}, EntityUid, bool)"/> for use with prediction. The local client will play
    /// animation to the recipient, and the server will play it to every other player in pvs range.
    /// </summary>
    public abstract void PlayPredicted(EntityUid uid, ProtoId<AnimationPrototype> animationId, EntityUid recipient, bool force = false);

    /// <summary>
    /// Stop animation on entity for every player in pvs range.
    /// </summary>
    /// <param name="uid">The UID of the entity.</param>
    /// <param name="animationKey">The key of the animation that will be stopped.</param>
    public abstract void Stop(EntityUid uid, string animationKey);

    /// <summary>
    /// Variant of <see cref="Stop(EntityUid, string)"/> that stop animation only to some specific client.
    /// </summary>
    public abstract void Stop(EntityUid uid, string animationKey, EntityUid recipient);

    /// <summary>
    /// Variant of <see cref="Stop(EntityUid, string)"/> that stop animation only to some specific client.
    /// </summary>
    public abstract void Stop(EntityUid uid, string animationKey, ICommonSession recipient);

    /// <summary>
    /// Filtered variant of <see cref="Stop(EntityUid, string)"/>, which should only be used
    /// if the filtering has to be more specific than simply pvs range based.
    /// </summary>
    public abstract void Stop(EntityUid uid, string animationKey, Filter filter);

    /// <summary>
    /// Variant of <see cref="Stop(EntityUid, string, EntityUid)"/> that only runs on the client, outside of prediction.
    /// Useful for shared code always run by both sides.
    /// </summary>
    public abstract void StopClient(EntityUid uid, string animationKey, EntityUid recipient);

    /// <summary>
    /// Variant of <see cref="Stop(EntityUid, string, EntityUid)"/> for use with prediction. The local client will stop
    /// animation to the recipient, and the server will stop it to every other player in pvs range.
    /// </summary>
    public abstract void StopPredicted(EntityUid uid, string animationKey, EntityUid recipient);

    #endregion
}

[Serializable, NetSerializable]
public sealed class PlayAnimationMessage(NetEntity animatedEntity, string animationId, bool force) : EntityEventArgs
{
    public NetEntity AnimatedEntity = animatedEntity;
    public string AnimationId = animationId;
    public bool Force = force;
}

[Serializable, NetSerializable]
public sealed class StopAnimationMessage(NetEntity animatedEntity, string animationKey) : EntityEventArgs
{
    public NetEntity AnimatedEntity = animatedEntity;
    public string AnimationKey = animationKey;
}
