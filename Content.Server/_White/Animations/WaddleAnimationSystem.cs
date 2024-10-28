using Content.Shared._White.Animations;
using Content.Shared.Movement.Components;

namespace Content.Server._White.Animations;

public sealed class WaddleAnimationSystem : SharedWaddledAnimationSystem
{
    protected override void PlayAnimation(EntityUid user)
    {
        RaiseNetworkEvent(new StartedWaddlingEvent(GetNetEntity(user)));
    }

    protected override void StopAnimation(EntityUid user)
    {
        RaiseNetworkEvent(new StoppedWaddlingEvent(GetNetEntity(user)));
    }
}
