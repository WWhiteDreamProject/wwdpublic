using Content.Shared.PDA;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.PDA;

public sealed class PdaSystem : SharedPdaSystem
{
    // WD EDIT START
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PdaComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    protected override void OnComponentInit(EntityUid uid, PdaComponent pda, ComponentInit args)
    {
        base.OnComponentInit(uid, pda, args);

        pda.OpeningAnimation = new Animation
        {
            Length = pda.OpeningAnimationTime,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = PdaVisualLayers.State,
                    KeyFrames =
                    {
                        new (pda.OpeningSpriteState, 0f),
                    },
                },
                new AnimationTrackSpriteFlick
                {
                    LayerKey = PdaVisualLayers.Screen,
                    KeyFrames =
                    {
                        new (pda.ScreenOpeningSpriteState, 0f),
                    },
                },
            },
        };

        pda.ClosingAnimation = new Animation
        {
            Length = pda.ClosingAnimationTime,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = PdaVisualLayers.State,
                    KeyFrames =
                    {
                        new (pda.ClosingSpriteState, 0f),
                    },
                },
                new AnimationTrackSpriteFlick
                {
                    LayerKey = PdaVisualLayers.Screen,
                    KeyFrames =
                    {
                        new (pda.ScreenClosingSpriteState, 0f),
                    },
                },
            },
        };
    }

    private void OnAppearanceChange(Entity<PdaComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !Appearance.TryGetData<PdaState>(ent, PdaVisuals.State, out var state, args.Component))
            return;

        if (_animationPlayer.HasRunningAnimation(ent, PdaComponent.AnimationKey))
            _animationPlayer.Stop(ent.Owner, PdaComponent.AnimationKey);

        switch (state)
        {
            case PdaState.Closed:
            {
                _sprite.LayerSetRsiState((ent.Owner, args.Sprite), PdaVisualLayers.State, ent.Comp.ClosedSpriteState);
                _sprite.LayerSetRsiState((ent.Owner, args.Sprite), PdaVisualLayers.Screen, ent.Comp.ScreenClosedSpriteState);
                return;
            }
            case PdaState.Closing:
            {
                if (ent.Comp.ClosingAnimationTime == TimeSpan.Zero)
                    return;

                _animationPlayer.Play(ent, (Animation) ent.Comp.ClosingAnimation, PdaComponent.AnimationKey);
                return;
            }
            case PdaState.Open:
            {
                _sprite.LayerSetRsiState((ent.Owner, args.Sprite), PdaVisualLayers.State, ent.Comp.OpenSpriteState);
                _sprite.LayerSetRsiState((ent.Owner, args.Sprite), PdaVisualLayers.Screen, ent.Comp.ScreenOpenSpriteState);
                return;
            }
            case PdaState.Opening:
            {
                if (ent.Comp.OpeningAnimationTime == TimeSpan.Zero)
                    return;

                _animationPlayer.Play(ent, (Animation) ent.Comp.OpeningAnimation, PdaComponent.AnimationKey);
                return;
            }
        }
    }
    // WD EDIT END
}
