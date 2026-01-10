using Content.Client.PDA;
using Content.Shared._White.PDA.Animation;
using Content.Shared.PDA;
using Robust.Client.GameObjects;

namespace Content.Client._White.PDA.Animation;

/// <summary>
/// System for handling PDA opening/closing animations
/// </summary>
public sealed class PdaAnimatedVisualizerSystem : VisualizerSystem<PdaAnimatedComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PdaAnimatedComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<PdaAnimationState>(uid, PdaVisuals.AnimationState, out var animState, args.Component))
            return;

        // Enclosure (PDA case)
        if (args.Sprite.LayerMapTryGet(PdaVisualizerSystem.PdaVisualLayers.Base, out var baseLayer))
        {
            switch (animState)
            {
                case PdaAnimationState.Closed:
                    args.Sprite.LayerSetState(baseLayer, "pda");
                    args.Sprite.LayerSetAutoAnimated(baseLayer, false);
                    break;
                case PdaAnimationState.Opening:
                    args.Sprite.LayerSetState(baseLayer, "pda_turning-on");
                    args.Sprite.LayerSetAutoAnimated(baseLayer, true);
                    break;
                case PdaAnimationState.Open:
                    args.Sprite.LayerSetState(baseLayer, "pda_on");
                    args.Sprite.LayerSetAutoAnimated(baseLayer, false);
                    break;
                case PdaAnimationState.Closing:
                    args.Sprite.LayerSetState(baseLayer, "pda_shutdown");
                    args.Sprite.LayerSetAutoAnimated(baseLayer, true);
                    break;
            }
        }

        // Screen
        if (args.Sprite.LayerMapTryGet(PdaVisualizerSystem.PdaVisualLayers.Screen, out var screenLayer))
        {
            switch (animState)
            {
                case PdaAnimationState.Closed:
                    args.Sprite.LayerSetVisible(screenLayer, false);
                    break;
                case PdaAnimationState.Opening:
                    args.Sprite.LayerSetVisible(screenLayer, true);
                    args.Sprite.LayerSetState(screenLayer, "screen_turning-on");
                    args.Sprite.LayerSetAutoAnimated(screenLayer, true);
                    break;
                case PdaAnimationState.Open:
                    args.Sprite.LayerSetState(screenLayer, "screen_on");
                    args.Sprite.LayerSetAutoAnimated(screenLayer, false);
                    break;
                case PdaAnimationState.Closing:
                    args.Sprite.LayerSetState(screenLayer, "screen_shutdown");
                    args.Sprite.LayerSetAutoAnimated(screenLayer, true);
                    break;
            }
        }
    }
}
