using Content.Shared._White.Animations;
using Content.Shared.Movement.Components;

namespace Content.Server._White.Animations;

public sealed class WaddleAnimationSystem : SharedWaddleAnimationSystem
{
    protected override void PlayAnimation(EntityUid user)
    {
        RaiseNetworkEvent(new StartedWaddlingEvent(GetNetEntity(user)));
    }
}
