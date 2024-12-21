using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared._White.Plumbing;
using Content.Shared.Wires;
using Robust.Shared.Map.Components;

namespace Content.Server._White.Plumbing;


/// <summary>
/// Literally just CableVisSystem.
/// </summary>
public sealed partial class PlumbingPipeVisSystem : EntitySystem
{

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly NodeContainerSystem _node = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<PlumbingPipeVisComponent, NodeGroupsRebuilt>(UpdateAppearance);
    }

    private void UpdateAppearance(EntityUid uid, PlumbingPipeVisComponent cableVis, ref NodeGroupsRebuilt args)
    {
        if (!_node.TryGetNode(uid, cableVis.Node, out PipeNode? node))
            return;

        var transform = Transform(uid);
        if (!TryComp<MapGridComponent>(transform.GridUid, out var grid))
            return;

        var mask = WireVisDirFlags.None;
        var tile = grid.TileIndicesFor(transform.Coordinates);

        foreach (var reachable in node.ReachableNodes)
        {
            if (reachable is not PipeNode)
                continue;

            var otherTransform = Transform(reachable.Owner);
            var otherTile = grid.TileIndicesFor(otherTransform.Coordinates);
            var diff = otherTile - tile;

            mask |= diff switch
            {
                (0, 1) => WireVisDirFlags.North,
                (0, -1) => WireVisDirFlags.South,
                (1, 0) => WireVisDirFlags.East,
                (-1, 0) => WireVisDirFlags.West,
                _ => WireVisDirFlags.None
            };
        }

        _appearance.SetData(uid, WireVisVisuals.ConnectedMask, mask);
    }

}
