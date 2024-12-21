using Content.Shared.Buckle;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Robust.Shared.Timing;

namespace Content.Shared._White.Animations;

public abstract class SharedWaddleAnimationSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaddleAnimationComponent, MoveEvent>(OnMovementInput);
    }

    private void OnMovementInput(EntityUid uid, WaddleAnimationComponent component, MoveEvent args)
    {
        if (_standingState.IsDown(uid)
            || _gravity.IsWeightless(uid)
            || _buckle.IsBuckled(uid))
            return;

        PlayAnimation(uid);
    }

    protected abstract void PlayAnimation(EntityUid user);
}
