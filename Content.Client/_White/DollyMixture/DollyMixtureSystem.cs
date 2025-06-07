using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using System.Numerics;

namespace Content.Client._White.DollyMixture;

public sealed class DollyMixtureSystem : EntitySystem
{
    [Dependency] private readonly IResourceCache _res = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DollyMixtureComponent, ComponentInit>(OnInit);
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<DollyMixtureComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var dollymix, out var sprite, out var xform))
        {
            Angle angle = xform.LocalRotation + _eye.CurrentEye.Rotation;
            const float MinAngleDelta = MathF.PI / 180 * 0.01f;
            if (MathHelper.CloseTo(dollymix.LastAngle, angle, MinAngleDelta))
                continue;

            dollymix.LastAngle = angle;
            for (int i = 0; i < dollymix.LayerIndices.Count; i++)
            {
                sprite.LayerSetRotation(dollymix.LayerIndices[i], angle);
            }
        }
    }

    // more than half of this method is handling missing shit. gg.
    private void OnInit(EntityUid uid, DollyMixtureComponent comp, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            Log.Error($"Failed to get SpriteComponent for {ToPrettyString(uid)}. Removing DollyMixtureComponent.");
            RemComp<DollyMixtureComponent>(uid);
            return;
        }

        if (!TryComp<TransformComponent>(uid, out var xform))
        {
            Log.Error($"Failed to get SpriteComponent for {ToPrettyString(uid)}. Removing DollyMixtureComponent.");
            RemComp<DollyMixtureComponent>(uid);
            return;
        }

        RSIResource? RSIres = null;
        if (!string.IsNullOrEmpty(comp.RSIPath) && !_res.TryGetResource($"/Textures/{comp.RSIPath}", out RSIres))
        {
            Log.Error($"Failed to get RSI {$"/Textures/{comp.RSIPath}"} for a dolly mixture component. Removing component. ({ToPrettyString(uid)})");
            RemComp<DollyMixtureComponent>(uid);
            return;
        }

        var RSI = RSIres?.RSI ?? sprite.BaseRSI;
        if(RSI is null)
        {
            Log.Error($"No RSI specified for both DollyMixtureComponent and SpriteComponent. Removing DollyMixtureComponent. ({ToPrettyString(uid)})");
            RemComp<DollyMixtureComponent>(uid);
            return;
        }

        comp.RSI = RSI;
        comp.LayerIndices = new(comp.States.Count);

        for (int i = 0; i < comp.States.Count; i++)
        {
            string stateId = comp.States[i];
            for (int repeat = 0; repeat < comp.RepeatLayers; repeat++)
            {
                Vector2 layerOffset = comp.Offset / EyeManager.PixelsPerMeter + comp.LayerOffset / EyeManager.PixelsPerMeter * i;
                layerOffset += comp.LayerOffset * ((float)repeat / comp.RepeatLayers);

                int layerIndex = sprite.AddBlankLayer();
                sprite.LayerSetRSI(layerIndex, RSI);
                sprite.LayerSetState(layerIndex, stateId);
                sprite.LayerSetOffset(layerIndex, layerOffset);
                sprite.LayerSetScale(layerIndex, comp.LayerScale);
                sprite.LayerSetRotation(layerIndex, xform.LocalRotation + _eye.CurrentEye.Rotation);
                if (comp.DefaultShader is not null)
                    sprite.LayerSetShader(layerIndex, comp.DefaultShader); // crutch, need to assign a proper datadef for each dollymix layer instead of this.
                comp.LayerIndices.Add(layerIndex);

                if (sprite.BaseRSI?.TryGetState($"{stateId}-unshaded", out var unshadedState) ?? false) // todo: assign a proper datadef for each dollymix layer instead of this.
                {
                    layerIndex = sprite.AddBlankLayer();
                    sprite.LayerSetRSI(layerIndex, RSI);
                    sprite.LayerSetState(layerIndex, $"{stateId}-unshaded");
                    sprite.LayerSetShader(layerIndex, "unshaded");
                    sprite.LayerSetOffset(layerIndex, layerOffset);
                    sprite.LayerSetScale(layerIndex, comp.LayerScale);
                    sprite.LayerSetRotation(layerIndex, xform.LocalRotation + _eye.CurrentEye.Rotation);
                    comp.LayerIndices.Add(layerIndex);
                }
            }
        }
        //sprite.GranularLayersRendering = true;
    }
}
