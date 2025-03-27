using Content.Shared._White.Animations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Timing;

namespace Content.Client._White.Animations;

public sealed class FlipOnHitSystem : SharedFlipOnHitSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlippingComponent, AnimationCompletedEvent>(OnAnimationComplete);
        SubscribeAllEvent<FlipOnHitEvent>(ev => PlayAnimation(GetEntity(ev.User), GetEntity(ev.Target)));
    }

    private void OnAnimationComplete(Entity<FlippingComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != FlippingComponent.AnimationKey)
            return;

        PlayAnimation(args.Uid, ent);
    }

    protected override void PlayAnimation(EntityUid user, EntityUid target)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (TerminatingOrDeleted(target))
            return;

        if (_animationSystem.HasRunningAnimation(target, FlippingComponent.AnimationKey))
        {
            EnsureComp<FlippingComponent>(target);
            return;
        }

        RemComp<FlippingComponent>(target);

        var baseAngle = Angle.Zero;
        if (EntityManager.TryGetComponent(target, out SpriteComponent? sprite))
            baseAngle = sprite.Rotation;

        var degrees = baseAngle.Degrees;

        var animation = new Animation
        {
            Length = TimeSpan.FromMilliseconds(400),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees - 10), 0f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees + 180), 0.2f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees + 360), 0.2f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees), 0f)
                    }
                }
            }
        };

        _animationSystem.Play(target, animation, FlippingComponent.AnimationKey);
    }
}
