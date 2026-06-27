using Content.Shared._White.Abilities.Invoker;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;

namespace Content.Client._White.Abilities;

public sealed class InvokerSystem : EntitySystem
{
    private Dictionary<EntityUid, Dictionary<int, int>> _orbLayers = new();
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InvokerComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
        SubscribeLocalEvent<InvokerComponent, ComponentShutdown>(OnCompShutdown);
        SubscribeLocalEvent<InvokerComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<InvokerComponent> ent, ref ComponentStartup args)
    {
        _orbLayers[ent] = new Dictionary<int, int>();
    }

    private void OnAfterHandleState(Entity<InvokerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateOrbVisuals(ent, ent.Comp);
    }

    private void OnCompShutdown(Entity<InvokerComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            foreach (var layer in _orbLayers.GetValueOrDefault(ent, new Dictionary<int, int>()).Values)
            {
                sprite.RemoveLayer(layer);
            }
        }

        _orbLayers.Remove(ent);
    }

    private void UpdateOrbVisuals(Entity<InvokerComponent> ent, InvokerComponent component)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_orbLayers.TryGetValue(ent, out var layerDict))
        {
            layerDict = new Dictionary<int, int>();
            _orbLayers[ent] = layerDict;
        }

        int orbCount = Math.Min(component.CurrentOrbs.Count, 3);

        if (orbCount == 0)
        {
            foreach (var layer in layerDict.Values)
            {
                sprite.LayerSetVisible(layer, false);
            }
            return;
        }

        float totalWidth = (orbCount - 1) * 1f;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < orbCount; i++)
        {
            var orbType = component.CurrentOrbs[i];

            if (!layerDict.TryGetValue(i, out var layerIndex))
            {
                layerIndex = sprite.LayerMapReserveBlank($"orb_{i}");
                layerDict[i] = layerIndex;
            }

            SpriteSpecifier? orbSprite = orbType switch
            {
                OrbType.Quas => component.QuasSprite,
                OrbType.Wex => component.WexSprite,
                OrbType.Exort => component.ExortSprite,
                _ => null
            };

            if (orbSprite != null)
            {
                sprite.LayerSetSprite(layerIndex, orbSprite);
                var offset = component.OrbOffsets.ElementAtOrDefault(i);
                sprite.LayerSetOffset(layerIndex, offset);
                sprite.LayerSetScale(layerIndex, Vector2.One);
                sprite.LayerSetVisible(layerIndex, true);
                sprite.LayerSetShader(layerIndex, "unshaded");
            }
            else
            {
                sprite.LayerSetVisible(layerIndex, false);
            }
        }

        foreach (var kvp in layerDict)
        {
            if (kvp.Key >= orbCount)
            {
                sprite.LayerSetVisible(kvp.Value, false);
            }
        }
    }

    public void AnimateOrbs(float frameTime)
    {
        foreach (var (uid, layerDict) in _orbLayers)
        {
            if (!TryComp<InvokerComponent>(uid, out var comp) ||
                !TryComp<SpriteComponent>(uid, out var sprite))
                continue;

            var time = _timing.CurTime;

            foreach (var (index, layer) in layerDict)
            {
                if (index >= comp.CurrentOrbs.Count)
                    continue;

                var bobOffset = MathF.Sin((float) time.TotalSeconds * 2f + index) * 2f;
                var baseOffset = GetOrbOffset(index, comp.CurrentOrbs.Count);
                sprite.LayerSetOffset(layer, baseOffset + new Vector2(0, bobOffset));

                var pulse = 1f + MathF.Sin((float) time.TotalSeconds * 3f + index * 1.5f) * 0.05f;
                sprite.LayerSetScale(layer, Vector2.One * pulse);
            }
        }
    }

    private Vector2 GetOrbOffset(int index, int totalCount)
    {
        float totalWidth = (totalCount - 1) * 32f;
        float startX = -totalWidth / 2f;
        return new Vector2(startX + index * 32f, 0);
    }
}
