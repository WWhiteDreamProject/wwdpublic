using Content.Shared._White.Plumbing;
using Robust.Client.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.Plumbing;

public sealed class PlumbingSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlumbingInputOutputVisualiserComponent, ComponentInit>(OnInputOutputVisualiserInit);
        SubscribeLocalEvent<PlumbingInputOutputVisualiserComponent, MoveEvent>(OnInputOutputVisualiserMove);

    }
    public void UpdatePipeLayer(SpriteComponent comp, object key, Direction dir)
    {
        if (dir == Direction.Invalid)
        {
            comp.LayerSetVisible(key, false);
            return;
        }
        comp.LayerSetVisible(key, true);

        UpdatePipeLayer(comp, key, dir.ToAngle());
    }
    public void UpdatePipeLayer(SpriteComponent comp, object key, Angle angle)
    {
        comp.LayerSetRotation(key, angle); // something fishy goin' on 'ere
    }

    private void OnInputOutputVisualiserInit(EntityUid uid, PlumbingInputOutputVisualiserComponent comp, ComponentInit args)
    {
        var spriteComp = Comp<SpriteComponent>(uid);
        spriteComp.LayerSetRenderingStrategy(0, LayerRenderingStrategy.NoRotation);
        
        spriteComp.LayerMapSet(PlumbingInputOutputLayers.InputLayer, spriteComp.AddLayerState("base", "_White/Structures/Machines/Plumbing/machinepipe.rsi", 0));
        spriteComp.LayerMapSet(PlumbingInputOutputLayers.OutputLayer, spriteComp.AddLayerState("base", "_White/Structures/Machines/Plumbing/machinepipe.rsi", 0));

        spriteComp.LayerSetColor(PlumbingInputOutputLayers.InputLayer, new Color(255, 111, 111));
        spriteComp.LayerSetColor(PlumbingInputOutputLayers.OutputLayer, new Color(111, 111, 255));
        UpdatePipeLayer(spriteComp, PlumbingInputOutputLayers.InputLayer, comp.InputDir);
        UpdatePipeLayer(spriteComp, PlumbingInputOutputLayers.OutputLayer, comp.OutputDir);

        //var xform = Comp<TransformComponent>(uid);
        //comp.InputAngleDiff = xform.LocalRotation - comp.InputDir.ToAngle();
        //comp.OutputAngleDiff = xform.LocalRotation - comp.OutputDir.ToAngle();
        //
        //UpdatePipeLayer(spriteComp, PlumbingInputOutputLayers.InputLayer, xform.LocalRotation + comp.InputAngleDiff);
        //UpdatePipeLayer(spriteComp, PlumbingInputOutputLayers.OutputLayer, xform.LocalRotation + comp.OutputAngleDiff);

    }
    private void OnInputOutputVisualiserAutoState(EntityUid uid, PlumbingInputOutputVisualiserComponent comp, ref AfterAutoHandleStateEvent args)
    {
        var spriteComp = Comp<SpriteComponent>(uid);
        UpdatePipeLayer(spriteComp, PlumbingInputOutputLayers.InputLayer, comp.InputDir);
        UpdatePipeLayer(spriteComp, PlumbingInputOutputLayers.OutputLayer, comp.OutputDir);
    }

    private void OnInputOutputVisualiserMove(EntityUid uid, PlumbingInputOutputVisualiserComponent comp, ref MoveEvent args)
    {
        //var spriteComp = Comp<SpriteComponent>(uid);
        //UpdatePipeLayer(spriteComp, PlumbingInputOutputLayers.InputLayer, args.NewRotation/* + comp.InputAngleDiff*/);
        //UpdatePipeLayer(spriteComp, PlumbingInputOutputLayers.OutputLayer, args.NewRotation/* + comp.OutputAngleDiff*/);
    }
}

public enum PlumbingInputOutputLayers
{
    InputLayer,
    OutputLayer
}
