using Content.Shared._White.Animations.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Animations.Systems;

public abstract class SharedWhiteAnimationPlayerSystem : EntitySystem
{
    public abstract void Play(EntityUid uid, ProtoId<AnimationPrototype> animationId);
}

[Serializable, NetSerializable]
public sealed class PlayAnimationMessage(NetEntity animatedEntity, string animationId) : EntityEventArgs
{
    public NetEntity AnimatedEntity = animatedEntity;
    public string AnimationId = animationId;
}
