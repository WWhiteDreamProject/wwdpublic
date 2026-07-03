using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared.IconSmoothing;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.IconSmoothing;


#pragma warning disable CS0618 // go fuck yourself
public sealed partial class IconSmoothSystem : EntitySystem
{
    private void CalculateNewSpriteCorners(Entity<IconSmoothComponent, SpriteComponent> entity, int layerIndex, MapGridComponent? grid, TransformComponent xform)
    {
        var smooth = entity.Comp1;
        var sprite = entity.Comp2;

        var (cornerNE, cornerNW, cornerSW, cornerSE) = grid == null
            ? (CornerFill.None, CornerFill.None, CornerFill.None, CornerFill.None)
            : CalculateCornerFill(grid, smooth, xform);

        // // TODO figure out a better way to set multiple sprite layers.
        // // This will currently re-calculate the sprite bounding box 4 times.
        // // It will also result in 4-8 sprite update events being raised when it only needs to be 1-2.
        // // At the very least each event currently only queues a sprite for updating.
        // // Oh god sprite component is a mess.
        sprite.LayerSetState(CornerLayers.NE, $"{smooth.StateBase}{(int) cornerNE}");
        sprite.LayerSetState(CornerLayers.SE, $"{smooth.StateBase}{(int) cornerSE}");
        sprite.LayerSetState(CornerLayers.SW, $"{smooth.StateBase}{(int) cornerSW}");
        sprite.LayerSetState(CornerLayers.NW, $"{smooth.StateBase}{(int) cornerNW}");
    }

    private (CornerFill ne, CornerFill nw, CornerFill sw, CornerFill se) CalculateCornerFill(MapGridComponent grid, IconSmoothComponent smooth, TransformComponent xform)
    {
        var pos = grid.TileIndicesFor(xform.Coordinates);
        var n = MatchingEntity(smooth, grid, pos, Direction.North, xform.LocalRotation);
        var ne = MatchingEntity(smooth, grid, pos, Direction.NorthEast, xform.LocalRotation);
        var e = MatchingEntity(smooth, grid, pos, Direction.East, xform.LocalRotation);
        var se = MatchingEntity(smooth, grid, pos, Direction.SouthEast, xform.LocalRotation);
        var s = MatchingEntity(smooth, grid, pos, Direction.South, xform.LocalRotation);
        var sw = MatchingEntity(smooth, grid, pos, Direction.SouthWest, xform.LocalRotation);
        var w = MatchingEntity(smooth, grid, pos, Direction.West, xform.LocalRotation);
        var nw = MatchingEntity(smooth, grid, pos, Direction.NorthWest, xform.LocalRotation);

        // ReSharper disable InconsistentNaming
        var cornerNE = CornerFill.None;
        var cornerSE = CornerFill.None;
        var cornerSW = CornerFill.None;
        var cornerNW = CornerFill.None;
        // ReSharper restore InconsistentNaming

        if (n)
        {
            cornerNE |= CornerFill.CounterClockwise;
            cornerNW |= CornerFill.Clockwise;
        }

        if (ne)
        {
            cornerNE |= CornerFill.Diagonal;
        }

        if (e)
        {
            cornerNE |= CornerFill.Clockwise;
            cornerSE |= CornerFill.CounterClockwise;
        }

        if (se)
        {
            cornerSE |= CornerFill.Diagonal;
        }

        if (s)
        {
            cornerSE |= CornerFill.Clockwise;
            cornerSW |= CornerFill.CounterClockwise;
        }

        if (sw)
        {
            cornerSW |= CornerFill.Diagonal;
        }

        if (w)
        {
            cornerSW |= CornerFill.Clockwise;
            cornerNW |= CornerFill.CounterClockwise;
        }

        if (nw)
        {
            cornerNW |= CornerFill.Diagonal;
        }

        return (cornerNE, cornerNW, cornerSW, cornerSE);
    }

    private void CalculateNewSpriteCardinal(Entity<IconSmoothComponent, SpriteComponent> entity, int layerIndex, MapGridComponent? grid, TransformComponent xform)
    {
        var dirs = CardinalConnectDirs.None;
        var smooth = entity.Comp1;
        var sprite = entity.Comp2;

        if (grid == null)
        {
            sprite.LayerSetState(layerIndex, $"{smooth.StateBase}{(int) dirs}");
            return;
        }

        var pos = grid.TileIndicesFor(xform.Coordinates);
        if (MatchingEntity(smooth, grid, pos, Direction.North))
            dirs |= CardinalConnectDirs.North;
        if (MatchingEntity(smooth, grid, pos, Direction.South))
            dirs |= CardinalConnectDirs.South;
        if (MatchingEntity(smooth, grid, pos, Direction.East))
            dirs |= CardinalConnectDirs.East;
        if (MatchingEntity(smooth, grid, pos, Direction.West))
            dirs |= CardinalConnectDirs.West;

        sprite.LayerSetState(layerIndex, $"{smooth.StateBase}{(int) dirs}");
    }

    private void CalculateNewSpriteDiagonal(Entity<IconSmoothComponent, SpriteComponent> entity, int layerIndex, MapGridComponent? grid, TransformComponent xform)
    {
        var smooth = entity.Comp1;
        var sprite = entity.Comp2;

        if (grid == null)
        {
            sprite.LayerSetState(layerIndex, $"{smooth.StateBase}0");
            return;
        }
        var neighbors = new Direction[]
        {
            Direction.East,
            Direction.SouthEast,
            Direction.South,
        };

        var pos = grid.TileIndicesFor(xform.Coordinates);
        var rotation = xform.LocalRotation;
        var matching = true;

        for (var i = 0; i < neighbors.Length; i++)
        {
            matching &= MatchingEntity(smooth, grid, pos, neighbors[i], rotation);
            if (!matching)
                break;
        }

        if (matching)
            sprite.LayerSetState(layerIndex, $"{smooth.StateBase}1");
        else
            sprite.LayerSetState(layerIndex, $"{smooth.StateBase}0");
    }

    private void CalculateNewSpriteHorizontal(Entity<IconSmoothComponent, SpriteComponent> entity, int layerIndex, MapGridComponent? grid, TransformComponent xform)
    {
        var dirs = CardinalConnectDirs.None;
        var smooth = entity.Comp1;
        var sprite = entity.Comp2;

        if (grid == null)
        {
            sprite.LayerSetState(layerIndex, $"{smooth.StateBase}{(int) dirs}");
            return;
        }
        var ourDir = xform.LocalRotation.GetDir();

        var pos = grid.TileIndicesFor(xform.Coordinates);
        if (MatchingEntity(smooth, grid, pos, Direction.East, xform.LocalRotation, sameRotPredicate))
            dirs |= CardinalConnectDirs.East;
        if (MatchingEntity(smooth, grid, pos, Direction.West, xform.LocalRotation, sameRotPredicate))
            dirs |= CardinalConnectDirs.West;

        sprite.LayerSetState(layerIndex, $"{smooth.StateBase}{(int) dirs}");

        bool sameRotPredicate(EntityUid uid) => Transform(uid).LocalRotation.GetDir() == ourDir;
    }

    private void CalculateNewSpriteVertical(Entity<IconSmoothComponent, SpriteComponent> entity, int layerIndex, MapGridComponent? grid, TransformComponent xform)
    {
        var dirs = CardinalConnectDirs.None;
        var smooth = entity.Comp1;
        var sprite = entity.Comp2;

        if (grid == null)
        {
            sprite.LayerSetState(layerIndex, $"{smooth.StateBase}{(int) dirs}");
            return;
        }
        var ourDir = xform.LocalRotation.GetDir();

        var pos = grid.TileIndicesFor(xform.Coordinates);
        if (MatchingEntity(smooth, grid, pos, Direction.North, xform.LocalRotation, sameRotPredicate))
            dirs |= CardinalConnectDirs.North;
        if (MatchingEntity(smooth, grid, pos, Direction.South, xform.LocalRotation, sameRotPredicate))
            dirs |= CardinalConnectDirs.South;

        sprite.LayerSetState(layerIndex, $"{smooth.StateBase}{(int) dirs}");

        bool sameRotPredicate(EntityUid uid) => Transform(uid).LocalRotation.GetDir() == ourDir;
    }

}
#pragma warning restore CS0618
