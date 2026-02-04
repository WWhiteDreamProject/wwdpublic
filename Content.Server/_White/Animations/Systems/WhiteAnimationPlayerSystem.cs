using Content.Shared._White.Animations.Prototypes;
using Content.Shared._White.Animations.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Animations.Systems;

public sealed class WhiteAnimationPlayerSystem : SharedWhiteAnimationPlayerSystem
{
    public override void Play(EntityUid uid, ProtoId<AnimationPrototype> animationId) =>
        RaiseNetworkEvent(new PlayAnimationMessage(GetNetEntity(uid), animationId));
}
