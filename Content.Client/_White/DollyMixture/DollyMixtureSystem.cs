using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Robust.Client.Graphics.RSI;

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

    protected override void PostInject() => base.PostInject();

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<DollyMixtureComponent, SpriteComponent, TransformComponent>();
        while(query.MoveNext(out var uid, out var dollymix, out var sprite, out var xform))
        {
            Angle angle;
            if (dollymix.AngleSnapFactor > 0)
                angle = Math.Round((xform.LocalRotation + _eye.CurrentEye.Rotation) * dollymix.AngleSnapFactor) / dollymix.AngleSnapFactor;
            else
                angle = xform.LocalRotation + _eye.CurrentEye.Rotation;

            for (int i = 0; i < dollymix.LayerIndices.Count; i++)
                sprite.LayerSetRotation(dollymix.LayerIndices[i], angle);
        }
    }

    private void OnInit(EntityUid uid, DollyMixtureComponent comp, ComponentInit args)
    {
        DebugTools.Assert(TryComp<SpriteComponent>(uid, out var sprite));

        RSIResource? RSIres = null;
        if(!string.IsNullOrEmpty(comp.RSIPath) && !_res.TryGetResource($"/Textures/{comp.RSIPath}", out RSIres))
        {
            RemComp<DollyMixtureComponent>(uid);
            return;
        }

        var RSI = RSIres?.RSI ?? sprite.BaseRSI;
        comp.RSI = RSI;
        comp.LayerIndices = new(comp.States.Count);

        for (int i = 0; i < comp.States.Count; i++)
        {
            string stateId = comp.States[i];
            
            int layerIndex = sprite.AddBlankLayer();
            sprite.LayerSetRSI(layerIndex, RSI);
            sprite.LayerSetState(layerIndex, stateId);
            //sprite.LayerSetRenderingStrategy(layerIndex, LayerRenderingStrategy.NoRotation);
            sprite.LayerSetOffset(layerIndex, comp.Offset / EyeManager.PixelsPerMeter + comp.LayerOffset / EyeManager.PixelsPerMeter * i);
            comp.LayerIndices.Add(layerIndex);

            if(sprite.BaseRSI?.TryGetState($"{stateId}-unshaded", out var unshadedState) ?? false)
            {
                layerIndex = sprite.AddBlankLayer();
                sprite.LayerSetRSI(layerIndex, RSI);
                sprite.LayerSetState(layerIndex, $"{stateId}-unshaded");
                //sprite.LayerSetRenderingStrategy(layerIndex, LayerRenderingStrategy.NoRotation);
                sprite.LayerSetShader(layerIndex, "unshaded");
                sprite.LayerSetOffset(layerIndex, comp.Offset / EyeManager.PixelsPerMeter + comp.LayerOffset / EyeManager.PixelsPerMeter * i);
                comp.LayerIndices.Add(layerIndex);
            }
        }
        sprite.GranularLayersRendering = true;
        //sprite.NoRotation = true;
    }
}
