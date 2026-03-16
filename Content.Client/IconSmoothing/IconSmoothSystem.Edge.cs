using System.Numerics;
using Content.Shared.IconSmoothing;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Linq;

namespace Content.Client.IconSmoothing;

public sealed partial class IconSmoothSystem
{
    // Handles drawing edge sprites on the non-smoothed edges.

    private void InitializeEdge()
    {
        SubscribeLocalEvent<SmoothEdgeComponent, ComponentStartup>(OnEdgeStartup);
        SubscribeLocalEvent<SmoothEdgeComponent, ComponentShutdown>(OnEdgeShutdown);
    }

    private void OnEdgeStartup(EntityUid uid, SmoothEdgeComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var baseOffsets = component.DisableBaseOffset
            ? new Dictionary<EdgeLayer, Vector2>
            {
                { EdgeLayer.South, Vector2.Zero },
                { EdgeLayer.East, Vector2.Zero },
                { EdgeLayer.North, Vector2.Zero },
                { EdgeLayer.West, Vector2.Zero },
                { EdgeLayer.SouthEast, Vector2.Zero },
                { EdgeLayer.NorthEast, Vector2.Zero },
                { EdgeLayer.NorthWest, Vector2.Zero },
                { EdgeLayer.SouthWest, Vector2.Zero }
            }
            : new Dictionary<EdgeLayer, Vector2>
            {
                { EdgeLayer.South, new Vector2(0, -1f) },
                { EdgeLayer.East, new Vector2(1f, 0f) },
                { EdgeLayer.North, new Vector2(0, 1f) },
                { EdgeLayer.West, new Vector2(-1f, 0f) },
                { EdgeLayer.SouthEast, new Vector2(1f, -1f) },
                { EdgeLayer.NorthEast, new Vector2(1f, 1f) },
                { EdgeLayer.NorthWest, new Vector2(-1f, 1f) },
                { EdgeLayer.SouthWest, new Vector2(-1f, -1f) }
            };

        foreach (var (edgeLayer, offset) in baseOffsets)
        {
            if (sprite.LayerMapTryGet(edgeLayer, out _))
            {
                sprite.LayerSetOffset(edgeLayer, offset);
                sprite.LayerSetVisible(edgeLayer, false);
            }
        }
    }

    private void OnEdgeShutdown(EntityUid uid, SmoothEdgeComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var allEdgeLayers = Enum.GetValues<EdgeLayer>();
        foreach (var edgeLayer in allEdgeLayers)
        {
            if (sprite.LayerMapTryGet(edgeLayer, out _))
                sprite.LayerMapRemove(edgeLayer);
        }
    }

    private void CalculateEdge(EntityUid uid, DirectionFlag directions, SpriteComponent? sprite = null, SmoothEdgeComponent? component = null)
    {
        if (!Resolve(uid, ref sprite, ref component, false))
            return;

        if (component.DrawDepth.HasValue)
            sprite.DrawDepth = component.DrawDepth.Value;

        var xform = Transform(uid);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var pos = _mapSystem.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates);
        var smoothQuery = GetEntityQuery<IconSmoothComponent>();

        // WWDP edit start
        if (component.BlockAdditionalKeys.Count > 0)
        {
            var sameTileEnumerator = grid.GetAnchoredEntitiesEnumerator(pos);
            while (sameTileEnumerator.MoveNext(out var sameTileEntity))
            {
                if (sameTileEntity == uid) continue;

                if (smoothQuery.TryGetComponent(sameTileEntity, out var sameTileSmooth) &&
                    sameTileSmooth.SmoothKey != null &&
                    component.BlockAdditionalKeys.Contains(sameTileSmooth.SmoothKey))
                {
                    // Скрываем все edge-слои
                    var allEdgeLayers = Enum.GetValues<EdgeLayer>();
                    foreach (var edgeLayer in allEdgeLayers)
                    {
                        if (sprite.LayerMapTryGet(edgeLayer, out var layerIndex))
                        {
                            sprite.LayerSetVisible(layerIndex, false);
                        }
                    }
                    return;
                }
            }
        }
        // WWDP edit end

        // All 8 directions
        var directionMappings = new[]
        {
            (DirectionFlag.South, EdgeLayer.South),
            (DirectionFlag.East, EdgeLayer.East),
            (DirectionFlag.North, EdgeLayer.North),
            (DirectionFlag.West, EdgeLayer.West),
            (DirectionFlag.SouthEast, EdgeLayer.SouthEast),
            (DirectionFlag.NorthEast, EdgeLayer.NorthEast),
            (DirectionFlag.NorthWest, EdgeLayer.NorthWest),
            (DirectionFlag.SouthWest, EdgeLayer.SouthWest)
        };

        foreach (var (dir, edge) in directionMappings)
        {
            if (!sprite.LayerMapTryGet(edge, out var layerIndex))
                continue;

            var neighborPos = pos + DirectionToOffset(dir);
            var hasMatchingNeighbor = false;
            var enumerator = grid.GetAnchoredEntitiesEnumerator(neighborPos);

            while (enumerator.MoveNext(out var neighbor))
            {
                if (smoothQuery.TryGetComponent(neighbor, out var neighborSmooth) &&
                    neighborSmooth != null &&
                    neighborSmooth.Enabled &&
                    MatchesEdgeCriteria(component, neighborSmooth))
                {
                    hasMatchingNeighbor = true;
                    break;
                }
            }

            // If RequireMatchingKey: show edge only when neighbor matches; otherwise show when no match (legacy)
            var shouldShowEdge = component.RequireMatchingKey
                ? hasMatchingNeighbor
                : !hasMatchingNeighbor;

            sprite.LayerSetVisible(layerIndex, shouldShowEdge);
        }
    }

    // WWDP edit start
    private void HideAllEdgeLayers(SpriteComponent sprite)
    {
        var allEdgeLayers = Enum.GetValues<EdgeLayer>();
        foreach (var edgeLayer in allEdgeLayers)
        {
            if (sprite.LayerMapTryGet(edgeLayer, out var layerIndex))
            {
                sprite.LayerSetVisible(layerIndex, false);
            }
        }
    }
    // WWDP edit end

    private bool MatchesEdgeCriteria(SmoothEdgeComponent edge, IconSmoothComponent neighbor)
    {
        if (!edge.RequireMatchingKey)
            return true; // legacy: always show edge

        if (neighbor.SmoothKey == null)
            return false;

        return edge.EdgeAdditionalKeys.Contains(neighbor.SmoothKey);
    }

    private Vector2i DirectionToOffset(DirectionFlag direction)
    {
        return direction switch
        {
            DirectionFlag.North => new Vector2i(0, 1),
            DirectionFlag.South => new Vector2i(0, -1),
            DirectionFlag.East => new Vector2i(1, 0),
            DirectionFlag.West => new Vector2i(-1, 0),
            DirectionFlag.NorthEast => new Vector2i(1, 1),
            DirectionFlag.NorthWest => new Vector2i(-1, 1),
            DirectionFlag.SouthEast => new Vector2i(1, -1),
            DirectionFlag.SouthWest => new Vector2i(-1, -1),
            _ => Vector2i.Zero
        };
    }

    private EdgeLayer GetEdge(DirectionFlag direction)
    {
        return direction switch
        {
            DirectionFlag.South => EdgeLayer.South,
            DirectionFlag.East => EdgeLayer.East,
            DirectionFlag.North => EdgeLayer.North,
            DirectionFlag.West => EdgeLayer.West,
            DirectionFlag.SouthEast => EdgeLayer.SouthEast,
            DirectionFlag.NorthEast => EdgeLayer.NorthEast,
            DirectionFlag.NorthWest => EdgeLayer.NorthWest,
            DirectionFlag.SouthWest => EdgeLayer.SouthWest,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    private enum EdgeLayer : byte
    {
        South,
        East,
        North,
        West,
        SouthEast,
        NorthEast,
        NorthWest,
        SouthWest
    }
}
