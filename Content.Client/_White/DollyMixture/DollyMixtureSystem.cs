using Content.Shared._White.DollyMixture;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Client._White.DollyMixture;

public sealed class DollyMixtureSystem : SharedDollyMixtureSystem
{
    [Dependency] private readonly IResourceCache _res = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DollyMixtureComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DollyMixtureComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<DollyMixtureComponent, AfterAutoHandleStateEvent>(OnAutoState);
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

    private void UpdateDollyMixture(EntityUid uid, DollyMixtureComponent comp)
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
    }

    private void OnRemove(EntityUid uid, DollyMixtureComponent comp, ComponentRemove args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        RemoveLayers(uid, comp);
    }

    private void OnInit(EntityUid uid, DollyMixtureComponent comp, ComponentInit args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite)) // unlike OnRemove() and RemoveLayers(), this doesn't get executed when placing a prototype
        {                                                   // i cry
            Log.Error($"Failed to get SpriteComponent for {ToPrettyString(uid)}. Removing DollyMixtureComponent.");
            RemComp<DollyMixtureComponent>(uid);
            return;
        }

        var xform = Transform(uid);
        sprite.NoRotation = true;

        if (comp.RSIPath is not null)
            BuildLayers(uid, comp, sprite);
    }

    public override void Apply3D(EntityUid uid, string RsiPath, string? statePrefix = null, Vector2? layerOffset = null, DollyMixtureComponent? comp = null)
    {
        comp ??= EnsureComp<DollyMixtureComponent>(uid);

        base.Apply3D(uid, RsiPath, statePrefix, layerOffset, comp);
        UpdateDollyMixture(uid, comp);
    }

    public override void Remove3D(EntityUid uid, DollyMixtureComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        base.Remove3D(uid, comp);
        UpdateDollyMixture(uid, comp);
    }

    private void RemoveLayers(EntityUid uid, DollyMixtureComponent comp)
    {
        SpriteComponent? sprite = null;
        if (!Resolve(uid, ref sprite, false)) // this gets executed after simply placing a prototype with this comp
            return;                           // i assume it is some prediction-related bullshit
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

        int i = 1;
        while (RSI.TryGetState($"{comp.StatePrefix}{i}", out var state))
        {
            for (int repeat = 0; repeat <= comp.RepeatLayers; repeat++)
            {
                float fraction = comp.RepeatLayers > 0 ? (float) repeat / comp.RepeatLayers : 0f;

                Vector2 layerOffset = comp.Offset / EyeManager.PixelsPerMeter + comp.LayerOffset / EyeManager.PixelsPerMeter * (i - 1 + fraction);

                int layerIndex = sprite.AddBlankLayer();
                sprite.LayerSetRSI(layerIndex, RSI);
                sprite.LayerSetState(layerIndex, state.StateId);
                sprite.LayerSetOffset(layerIndex, layerOffset);
                sprite.LayerSetRotation(layerIndex, xform.LocalRotation + _eye.CurrentEye.Rotation);
                if (comp.DefaultShader is string defaultshader)
                    sprite.LayerSetShader(layerIndex, defaultshader); // crutch for customghosts
                string layerMap = $"dmm-{comp.StatePrefix}{i}({repeat}/{comp.RepeatLayers})";
                sprite.LayerMapSet(layerMap, layerIndex);
                comp.LayerMappings.Add(layerMap);

                if (RSI.TryGetState($"{comp.StatePrefix}{i}-unshaded", out var unshadedState))
                {
                    layerIndex = sprite.AddBlankLayer();
                    sprite.LayerSetRSI(layerIndex, RSI);
                    sprite.LayerSetState(layerIndex, unshadedState.StateId);
                    sprite.LayerSetOffset(layerIndex, layerOffset);
                    sprite.LayerSetRotation(layerIndex, xform.LocalRotation + _eye.CurrentEye.Rotation);
                    sprite.LayerSetShader(layerIndex, "unshaded");
                    layerMap = $"{layerMap}u";
                    sprite.LayerMapSet(layerMap, layerIndex);
                    comp.LayerMappings.Add(layerMap);
                }
            }
            i++;
        }
        comp.CurrentRSIPath = comp.RSIPath;
    }
}

