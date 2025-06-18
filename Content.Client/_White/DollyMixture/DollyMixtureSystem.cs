using Content.Shared._White.DollyMixture;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed.Commands.Generic.Variables;
using Robust.Shared.Utility;
using System.Collections.Immutable;
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
            Angle angle = xform.LocalRotation + _eye.CurrentEye.Rotation;
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

        BuildLayers(uid, comp.RSIPath, comp);
        comp.CurrentRSIPath = comp.RSIPath;
    }

    private void OnRemove(EntityUid uid, DollyMixtureComponent comp, ComponentRemove args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        RemoveLayers(uid, comp);
    }

    // more than half of this method is handling missing shit. gg.
    private void OnInit(EntityUid uid, DollyMixtureComponent comp, ComponentInit args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            Log.Error($"Failed to get SpriteComponent for {ToPrettyString(uid)}. Removing DollyMixtureComponent.");
            RemComp<DollyMixtureComponent>(uid);
            return;
        }


        var xform = Transform(uid);
        sprite.NoRotation = true;

        if (comp.RSIPath is not null)
            BuildLayers(uid, comp.RSIPath, comp, sprite);
    }

    public override void Apply3D(EntityUid uid, string RsiPath, string? statePrefix = null, Vector2? layerOffset = null, DollyMixtureComponent? comp = null)
    {
        if(!Resolve(uid, ref comp))
            return;

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
        if (!Resolve(uid, ref sprite))
            return;
        foreach (var layerMapping in comp.LayerMappings)
            sprite.RemoveLayer(layerMapping);
        comp.CurrentRSIPath = null;
        comp.LayerMappings.Clear();
    }

    private void BuildLayers(EntityUid uid, string RsiPath, DollyMixtureComponent? comp = null, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref comp) || !Resolve(uid, ref sprite))
            return;

        var xform = Transform(uid);
        comp.CurrentRSIPath = RsiPath;
        RSIResource? RSIres = null;
        if (!string.IsNullOrEmpty(comp.RSIPath) && !_res.TryGetResource($"/Textures/{comp.RSIPath}", out RSIres))
        {
            Log.Error($"Failed to get RSI {$"/Textures/{comp.RSIPath}"} for a dolly mixture component.");
            return;
        }

        var RSI = RSIres?.RSI ?? sprite.BaseRSI;
        if (RSI is null)
        {
            Log.Error($"No RSI specified for both DollyMixtureComponent and SpriteComponent.");
            return;
        }

        int i = 1;
        while (RSI.TryGetState($"{comp.StatePrefix}{i}", out var state))
        {
            for (int repeat = 0; repeat < comp.RepeatLayers; repeat++)
            {
				Vector2 layerOffset = comp.Offset / EyeManager.PixelsPerMeter + comp.LayerOffset / EyeManager.PixelsPerMeter * (i - 1 + (float)repeat/RepeatLayers );

				int layerIndex = sprite.AddBlankLayer();
				sprite.LayerSetRSI(layerIndex, RSI);
				sprite.LayerSetState(layerIndex, state.StateId);
				sprite.LayerSetOffset(layerIndex, layerOffset);
				sprite.LayerSetRotation(layerIndex, xform.LocalRotation + _eye.CurrentEye.Rotation);
				string layerMap = $"dmm-{comp.StatePrefix}{i}";
				sprite.LayerMapSet(layerMap, layerIndex);
				comp.LayerMappings.Add(layerMap);

				if (RSI.TryGetState($"{comp.StatePrefix}{i}-unshaded", out var unshadedState))
				{
					layerIndex = sprite.AddBlankLayer();
					sprite.LayerSetRSI(layerIndex, RSI);
					sprite.LayerSetState(layerIndex, unshadedState.StateId);
					sprite.LayerSetOffset(layerIndex, layerOffset);
					sprite.LayerSetRotation(layerIndex, xform.LocalRotation + _eye.CurrentEye.Rotation);
					layerMap = $"dmm-{comp.StatePrefix}{i}u";
					sprite.LayerMapSet(layerMap, layerIndex);
					comp.LayerMappings.Add(layerMap);
				}
			}
            i++;
        }
    }
}

