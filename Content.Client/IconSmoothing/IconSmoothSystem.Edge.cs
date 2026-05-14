using System.Numerics;
using Content.Shared.IconSmoothing;
using Robust.Client.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Client.IconSmoothing;

public sealed partial class IconSmoothSystem
{
//    private void OnEdgeShutdown(EntityUid uid, SmoothEdgeComponent component, ComponentShutdown args)
//    {
//        if (!TryComp<SpriteComponent>(uid, out var sprite))
//            return;
//
//        sprite.LayerMapRemove(EdgeLayer.South);
//        sprite.LayerMapRemove(EdgeLayer.East);
//        sprite.LayerMapRemove(EdgeLayer.North);
//        sprite.LayerMapRemove(EdgeLayer.West);
//    }

    private void CalculateEdge(EntityUid uid, SpriteComponent? sprite = null, IconSmoothComponent? smooth = null)
    {
        if (!Resolve(uid, ref sprite, ref smooth, false))
            return;

        if (smooth.SmoothEdgeLayers.Length == 0)
            return;

        var xform = Transform(uid);

        var directions = EdgeLayer.None;

        if (xform.GridUid is EntityUid gridUid && TryComp<MapGridComponent>(gridUid, out var grid))
        {
            var pos = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);

            if (MatchingEntity(smooth, grid, pos, Direction.North, xform.LocalRotation, null, true))
                directions |= EdgeLayer.North;
            if (MatchingEntity(smooth, grid, pos, Direction.South, xform.LocalRotation, null, true))
                directions |= EdgeLayer.South;
            if (MatchingEntity(smooth, grid, pos, Direction.East, xform.LocalRotation, null, true))
                directions |= EdgeLayer.East;
            if (MatchingEntity(smooth, grid, pos, Direction.West, xform.LocalRotation, null, true))
                directions |= EdgeLayer.West;
            if (MatchingEntity(smooth, grid, pos, Direction.NorthEast, xform.LocalRotation, null, true))
                directions |= EdgeLayer.NorthEast;
            if (MatchingEntity(smooth, grid, pos, Direction.NorthWest, xform.LocalRotation, null, true))
                directions |= EdgeLayer.NorthWest;
            if (MatchingEntity(smooth, grid, pos, Direction.SouthEast, xform.LocalRotation, null, true))
                directions |= EdgeLayer.SouthEast;
            if (MatchingEntity(smooth, grid, pos, Direction.SouthWest, xform.LocalRotation, null, true))
                directions |= EdgeLayer.SouthWest;
        }

        UpdateEdge(uid, directions, sprite, smooth);
    }

    private void UpdateEdge(EntityUid uid, EdgeLayer directions, SpriteComponent? sprite = null, IconSmoothComponent? smooth = null)
    {
        if (!Resolve(uid, ref sprite, ref smooth, false))
            return;

        if (smooth.SmoothEdgeLayers.Length == 0)
            return;

        foreach (var edge in smooth.SmoothEdgeLayers)
        {
            var visible = (edge & directions) == 0x0;

            _sprite.LayerSetVisible((uid, sprite), edge, visible ^ smooth.ShowEdgeIfMatching);
        }
    }
}
