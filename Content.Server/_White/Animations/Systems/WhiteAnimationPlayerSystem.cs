using Content.Shared._White.Animations.Prototypes;
using Content.Shared._White.Animations.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Animations.Systems;

public sealed class WhiteAnimationPlayerSystem : SharedWhiteAnimationPlayerSystem
{
    #region Public API

    public override void Play(EntityUid uid, ProtoId<AnimationPrototype> animationId, bool force = false)
    {
        var filter = Filter.Pvs(uid, entityManager: EntityManager);
        RaiseNetworkEvent(new PlayAnimationMessage(GetNetEntity(uid), animationId, force), filter);
    }

    public override void Play(EntityUid uid, ProtoId<AnimationPrototype> animationId, EntityUid recipient, bool force = false)
    {
        if (!TryComp<ActorComponent>(recipient, out var actor))
            return;

        RaiseNetworkEvent(new PlayAnimationMessage(GetNetEntity(uid), animationId, force), actor.PlayerSession);
    }

    public override void Play(EntityUid uid, ProtoId<AnimationPrototype> animationId, ICommonSession recipient, bool force = false) =>
        RaiseNetworkEvent(new PlayAnimationMessage(GetNetEntity(uid), animationId, force), recipient);

    public override void Play(EntityUid uid, ProtoId<AnimationPrototype> animationId, Filter filter, bool force = false) =>
        RaiseNetworkEvent(new PlayAnimationMessage(GetNetEntity(uid), animationId, force), filter);

    public override void PlayClient(EntityUid uid, ProtoId<AnimationPrototype> animationId, EntityUid recipient, bool force = false)
    {
        // This is for the client
    }

    public override void PlayPredicted(EntityUid uid, ProtoId<AnimationPrototype> animationId, EntityUid recipient, bool force = false)
    {
        var filter = Filter.PvsExcept(recipient, entityManager: EntityManager);
        RaiseNetworkEvent(new PlayAnimationMessage(GetNetEntity(uid), animationId, force), filter);
    }

    public override void Stop(EntityUid uid, string animationkey)
    {
        var filter = Filter.Pvs(uid, entityManager: EntityManager);
        RaiseNetworkEvent(new StopAnimationMessage(GetNetEntity(uid), animationkey), filter);
    }

    public override void Stop(EntityUid uid, string animationkey, EntityUid recipient)
    {
        if (!TryComp<ActorComponent>(recipient, out var actor))
            return;

        RaiseNetworkEvent(new StopAnimationMessage(GetNetEntity(uid), animationkey), actor.PlayerSession);
    }

    public override void Stop(EntityUid uid, string animationkey, ICommonSession recipient) =>
        RaiseNetworkEvent(new StopAnimationMessage(GetNetEntity(uid), animationkey), recipient);

    public override void Stop(EntityUid uid, string animationkey, Filter filter) =>
        RaiseNetworkEvent(new StopAnimationMessage(GetNetEntity(uid), animationkey), filter);

    public override void StopClient(EntityUid uid, string animationkey, EntityUid recipient)
    {
        // This is for the client
    }

    public override void StopPredicted(EntityUid uid, string animationkey, EntityUid recipient)
    {
        var filter = Filter.PvsExcept(recipient, entityManager: EntityManager);
        RaiseNetworkEvent(new StopAnimationMessage(GetNetEntity(uid), animationkey), filter);
    }

    #endregion
}
