using Content.Shared._White.DollyMixture;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client._White.DollyMixture;

public sealed class DollyMixtureSystem : SharedDollyMixtureSystem
{
    [Dependency] private readonly IResourceCache _res = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private ShaderPrototype _voxelProto = default!;
    private ShaderPrototype _voxelProtoEmissive = default!;

    private ShaderInstance _voxelDefaultShader = default!;
    private ShaderInstance _voxelEmissiveDefaultShader = default!;

    private const float DefaultHeight = 0.75f;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DollyMixtureComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DollyMixtureComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<DollyMixtureComponent, AfterAutoHandleStateEvent>(OnAutoState);

        _voxelProto = _proto.Index<ShaderPrototype>("Voxel");
        _voxelProtoEmissive = _proto.Index<ShaderPrototype>("VoxelEmissive");

        _voxelDefaultShader = _voxelProto.InstanceUnique();
        _voxelDefaultShader.SetParameter("height", DefaultHeight);
        _voxelEmissiveDefaultShader = _voxelProtoEmissive.InstanceUnique();
        _voxelEmissiveDefaultShader.SetParameter("height", DefaultHeight);
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<DollyMixtureComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var dollymix, out var sprite, out var xform))
        {
            Angle angle = _xform.GetWorldRotation(xform) + _eye.CurrentEye.Rotation;
            if (dollymix.DirectionCount > 0)
                angle = Math.Round(angle / Math.Tau * dollymix.DirectionCount) * Math.Tau / dollymix.DirectionCount;

            const float MinAngleDelta = MathF.PI / 180 * 0.01f;
            if (MathHelper.CloseTo(dollymix.LastAngle, angle, MinAngleDelta))
                continue;

            dollymix.LastAngle = angle;
            for (int i = 0; i < dollymix.LayerMappings.Count; i++)
                sprite.LayerSetRotation(dollymix.LayerMappings[i], angle);
        }
    }

    private void OnAutoState(EntityUid uid, DollyMixtureComponent comp, AfterAutoHandleStateEvent args)
    {
        UpdateDollyMixture(uid, comp);
    }

    public void UpdateDollyMixture(EntityUid uid, DollyMixtureComponent comp)
    {
        if (comp.CurrentRSIPath == comp.RSIPath)
            return;

        if (comp.RSIPath is null)
        {
            RemoveLayers(uid, comp);
            return;
        }

        if (comp.CurrentRSIPath is not null)
            RemoveLayers(uid, comp);

        BuildLayers(uid, comp);
        comp.CurrentRSIPath = comp.RSIPath;
    }

    private void OnRemove(EntityUid uid, DollyMixtureComponent comp, ComponentRemove args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        RemoveLayers(uid, comp);
    }

    private void OnInit(EntityUid uid, DollyMixtureComponent comp, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite)) // unlike OnRemove() and RemoveLayers(), this doesn't get executed when placing a prototype
        {                                                   // i cry
            Log.Error($"Failed to get SpriteComponent for {ToPrettyString(uid)}. Removing DollyMixtureComponent.");
            RemComp<DollyMixtureComponent>(uid);
            return;
        }

        sprite.NoRotation = true;
        UpdateDollyMixture(uid, comp);
    }

    public override void Apply3D(Entity<DollyMixtureComponent?> entity, string rsiPath, string? statePrefix = null, Vector2? layerOffset = null)
    {
        entity.Comp ??= EnsureComp<DollyMixtureComponent>(entity);
        base.Apply3D(entity, rsiPath, statePrefix, layerOffset);
        UpdateDollyMixture(entity, entity.Comp);
    }

    public override void Remove3D(Entity<DollyMixtureComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        base.Remove3D(entity);
        UpdateDollyMixture(entity, entity.Comp);
    }

    private void RemoveLayers(EntityUid uid, DollyMixtureComponent comp)
    {
        //SpriteComponent? sprite = null;
        //if (!Resolve(uid, ref sprite, false)) // this gets executed after simply placing a prototype with this comp
        //    return;                           // i assume it is some prediction-related bullshit
        if(!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        foreach (var layerMapping in comp.LayerMappings)
            sprite.RemoveLayer(layerMapping);

        comp.CurrentRSIPath = null;
        comp.LayerMappings.Clear();
    }

    private void BuildLayers(EntityUid uid, DollyMixtureComponent comp, SpriteComponent? sprite = null)
    {
        if (string.IsNullOrEmpty(comp.RSIPath))
        {
            Log.Error($"An empty rsi path was passed to BuildLayers().");
            return;
        }

        if (!Resolve(uid, ref sprite, false))
            return;

        var xform = Transform(uid);
        if (!_res.TryGetResource($"/Textures/{comp.RSIPath}", out RSIResource? RSIres))
        {
            Log.Error($"Failed to get RSI {$"/Textures/{comp.RSIPath}"} for a dolly mixture component.");
            return;
        }

        var RSI = RSIres.RSI;

        ShaderInstance voxelShader;
        ShaderInstance voxelEmissiveShader;

        // caching shaders would probably make more sense
        // i don't want to bother with that, since 95% of models
        // will use default spacing
        // TODO: consider removing the ability to specify layer height altogether
        if(comp.LayerHeight != DefaultHeight)
        {
            voxelShader = _voxelProto.InstanceUnique();
            voxelShader.SetParameter("height", comp.LayerHeight);
            voxelEmissiveShader = _voxelProtoEmissive.InstanceUnique();
            voxelEmissiveShader.SetParameter("height", comp.LayerHeight);
        }
        else
        {
            voxelShader = _voxelDefaultShader;
            voxelEmissiveShader = _voxelEmissiveDefaultShader;
        }

        int i = 1;
        while (RSI.TryGetState($"{comp.StatePrefix}{i}", out var state))
        {
            Vector2 layerOffset = comp.Offset / EyeManager.PixelsPerMeter + new Vector2(0, comp.LayerHeight) / EyeManager.PixelsPerMeter * (i - 1);

            int layerIndex = sprite.AddBlankLayer();
            sprite.LayerSetRSI(layerIndex, RSI);
            sprite.LayerSetState(layerIndex, state.StateId);
            sprite.LayerSetOffset(layerIndex, layerOffset);
            sprite.LayerSetRotation(layerIndex, xform.LocalRotation + _eye.CurrentEye.Rotation);
            string layerMap = $"dollymix-{comp.StatePrefix}{i}";
            DebugTools.Assert(!sprite.LayerExists(layerMap), "Dollymix layer already present when building layers; improper cleanup?");
            sprite.LayerMapSet(layerMap, layerIndex);
            comp.LayerMappings.Add(layerMap);

            if (RSI.TryGetState($"{comp.StatePrefix}{i}-unshaded", out var unshadedState))
            {
                int unshadedLayerIndex = sprite.AddBlankLayer();
                sprite.LayerSetRSI(unshadedLayerIndex, RSI);
                sprite.LayerSetState(unshadedLayerIndex, unshadedState.StateId);
                string unshadedLayerMap = $"{layerMap}-u";
                sprite.LayerMapSet(unshadedLayerMap, unshadedLayerIndex);
                comp.LayerMappings.Add(unshadedLayerMap);

                sprite.TryGetLayer(unshadedLayerIndex, out var unshadedLayer);
                DebugTools.Assert(unshadedLayer is not null);

                SpriteComponent.CopyToShaderParameters ctsp = new(layerMap);
                ctsp.ParameterTexture = "emissiveTexture";
                ctsp.ParameterUV = "emissiveUV";
                unshadedLayer.CopyToShaderParameters = ctsp;

                sprite.LayerSetShader(layerIndex, voxelEmissiveShader);

            }
            else
            {
                sprite.LayerSetShader(layerIndex, voxelShader);
            }
            i++;
        }
    }
}

