using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using System.Linq;
//
//namespace Content.Server.NodeContainer.Nodes
//{
//    /// <summary>
//    ///     A <see cref="Node"/> that can reach other <see cref="AdjacentNode"/>s that are directly adjacent to it.
//    /// </summary>
//    [DataDefinition]
//    public sealed partial class AdjacentDirectionalNode : Node
//    {
//        const DirectionFlag alldirs = DirectionFlag.North | DirectionFlag.East | DirectionFlag.South | DirectionFlag.West;
//
//        [DataField]
//        DirectionFlag Direction = alldirs;
//
//        // yeah
//        public void RotateCounterClockwise()
//        {
//            Direction = (DirectionFlag) (((int) Direction << 1 | (int) Direction >> 4) & (int) alldirs);
//        }
//        // yeah...
//        public void RotateClockwise()
//        {
//            Direction = (DirectionFlag) (((int) Direction << 4 | (int) Direction >> 1) & (int) alldirs);
//        }
//
//        public override IEnumerable<Node> GetReachableNodes(TransformComponent xform,
//            EntityQuery<NodeContainerComponent> nodeQuery,
//            EntityQuery<TransformComponent> xformQuery,
//            MapGridComponent? grid,
//            IEntityManager entMan)
//        {
//            if (!xform.Anchored || grid == null)
//                yield break;
//
//            var gridIndex = grid.TileIndicesFor(xform.Coordinates);
//
//            foreach (var (dir, node) in NodeHelpers.GetCardinalNeighborNodes(nodeQuery, grid, gridIndex))
//            {
//                if (node != this &&
//                    dir != Robust.Shared.Maths.Direction.Invalid &&
//                    (dir.AsFlag() & Direction) != 0)
//                {
//                    yield return node;
//                }
//            }
//        }
//    }
//}



[NodeGroup(NodeGroupID.Plumbing)]
public sealed class PlumbingNodeGroup : BaseNodeGroup
{

    [ViewVariables]
    public EntityUid? Grid { get; private set; }

    [ViewVariables]
    public HashSet<Node> InputNodes = new();
    [ViewVariables]
    public HashSet<Node> OutputNodes = new();


    public override void Initialize(Node sourceNode, IEntityManager entMan)
    {
        base.Initialize(sourceNode, entMan);

        Grid = entMan.GetComponent<TransformComponent>(sourceNode.Owner).GridUid;

        //if (Grid == null)
        //{
        //    // This is probably due to a cannister or something like that being spawned in space.
        //    return;
        //}
        //
        //_atmosphereSystem = entMan.EntitySysManager.GetEntitySystem<AtmosphereSystem>();
        //_atmosphereSystem.AddPipeNet(Grid.Value, this);
    }

    public void Update()
    {
        //_atmosphereSystem?.React(Air, this);
    }

    public override void LoadNodes(List<Node> groupNodes)
    {
        base.LoadNodes(groupNodes);

        foreach (var _node in groupNodes)
        {
            var node = (PipeNode) _node;
            switch (node.Name.ToLower())
            {
                case "input":
                    DebugTools.Assert(InputNodes.Add(node), "Attempted to add an input node twice to the same node group");
                    break;
                case "output":
                    DebugTools.Assert(OutputNodes.Add(node), "Attempted to add an output node twice to the same node group");
                    break;
            }

        }
    }

    public override void RemoveNode(Node node)
    {
        base.RemoveNode(node);

        switch (node.Name.ToLower())
        {
            case "input":
                DebugTools.Assert(InputNodes.Remove(node), "Attempted to remove a non-member input node the node group");
                break;
            case "output":
                DebugTools.Assert(OutputNodes.Remove(node), "Attempted to remove a non-member output node the node group");
                break;

        }
    }

    public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
    {
        RemoveFromGridAtmos();

        //var newAir = new List<GasMixture>(newGroups.Count());
        //foreach (var newGroup in newGroups)
        //{
        //    if (newGroup.Key is IPipeNet newPipeNet)
        //        newAir.Add(newPipeNet.Air);
        //}
        //
        //_atmosphereSystem?.DivideInto(Air, newAir);
    }

    private void RemoveFromGridAtmos()
    {
        //if (Grid == null)
        //    return;
        //
        //_atmosphereSystem?.RemovePipeNet(Grid.Value, this);
    }

    public override string GetDebugData()
    {
        return "ass blast usa";
    }
}
