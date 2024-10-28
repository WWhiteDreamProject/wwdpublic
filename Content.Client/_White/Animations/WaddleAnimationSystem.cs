using System.Numerics;
using Content.Client.Buckle;
using Content.Client.Gravity;
using Content.Shared._White.Animations;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client._White.Animations;

public sealed class WaddleAnimationSystem : SharedWaddledAnimationSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly GravitySystem _gravity = default!;
    [Dependency] private readonly BuckleSystem _buckle = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WaddleAnimationComponent, AnimationCompletedEvent>(OnAnimationCompleted);

        SubscribeAllEvent<StartedWaddlingEvent>(ev => PlayAnimation(GetEntity(ev.User)));
        SubscribeAllEvent<StoppedWaddlingEvent>(ev => StopAnimation(GetEntity(ev.User)));
    }

    protected override void PlayAnimation(EntityUid uid)
    {
        if (!Timing.IsFirstTimePredicted
            || !TryComp<WaddleAnimationComponent>(uid, out var component)
            || !TryComp<InputMoverComponent>(uid, out var mover)
            || _animation.HasRunningAnimation(uid, component.KeyName)
            || _standingState.IsDown(uid)
            || _gravity.IsWeightless(uid)
            || _buckle.IsBuckled(uid))
            return;

        var tumbleIntensity = component.LastStep ? 360 - component.TumbleIntensity : component.TumbleIntensity;
        var len = mover.Sprinting ? component.AnimationLength * component.RunAnimationLengthMultiplier : component.AnimationLength;

        component.LastStep = !component.LastStep;
        component.IsCurrentlyWaddling = true;

        var animation = new Animation()
        {
            Length = TimeSpan.FromSeconds(len),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(0), 0),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(tumbleIntensity), len/3),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(0), len/3),
                    }
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(), 0),
                        new AnimationTrackProperty.KeyFrame(component.HopIntensity, len/3),
                        new AnimationTrackProperty.KeyFrame(new Vector2(), len/3),
                    }
                }
            }
        };

        _animation.Play(uid, animation, component.KeyName);
    }

    protected override void StopAnimation(EntityUid uid)
    {
        if (!TryComp<WaddleAnimationComponent>(uid, out var component)
            || !TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _animation.Stop(uid, component.KeyName);

        sprite.Offset = new Vector2();
        sprite.Rotation = Angle.FromDegrees(0);
        component.IsCurrentlyWaddling = false;
    }

    private void OnAnimationCompleted(EntityUid uid, WaddleAnimationComponent component, AnimationCompletedEvent args)
    {
        if (args.Key != component.KeyName)
            return;

        PlayAnimation(uid);
    }
}
