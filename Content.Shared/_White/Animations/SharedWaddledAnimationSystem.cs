using Content.Shared.Buckle;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Robust.Shared.Timing;

namespace Content.Shared._White.Animations;

public abstract class SharedWaddledAnimationSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaddleAnimationComponent, MoveInputEvent>(OnMovementInput);
    }

    private void OnMovementInput(EntityUid uid, WaddleAnimationComponent component, MoveInputEvent args)
    {
        if (!Timing.IsFirstTimePredicted
            || _standingState.IsDown(uid)
            || _gravity.IsWeightless(uid)
            || _buckle.IsBuckled(uid))
            return;

        if (!args.HasDirectionalMovement && component.IsCurrentlyWaddling)
        {
            component.IsCurrentlyWaddling = false;
            StopAnimation(uid);

            return;
        }

        if (component.IsCurrentlyWaddling || !args.HasDirectionalMovement)
            return;

        component.IsCurrentlyWaddling = true;

        PlayAnimation(uid);
    }

    protected abstract void PlayAnimation(EntityUid user);

    protected abstract void StopAnimation(EntityUid user);
}
