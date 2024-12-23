using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared._White.Misc.ChristmasLights;
using Content.Shared.Interaction;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._White.Misc;


public sealed class ChristmasLightsSystem : EntitySystem
{
   [Dependency] private readonly NodeGroupSystem _node = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChristmasLightsComponent, ActivateInWorldEvent>(OnChristmasLightsActivate);
        SubscribeLocalEvent<ChristmasLightsComponent, ComponentInit>(OnChristmasLightsInit);

    }

    private void OnChristmasLightsInit(EntityUid uid, ChristmasLightsComponent comp, ComponentInit args)
    {
        if(TryComp<NodeContainerComponent>(uid, out var cont))
        {
            if(cont.Nodes.TryGetValue("christmaslight", out var node))
                _node.QueueReflood(node);
        }
    }


    private void OnChristmasLightsActivate(EntityUid uid, ChristmasLightsComponent comp, ActivateInWorldEvent args)
    {
        if(TryComp<NodeContainerComponent>(uid, out var cont) && cont.Nodes.TryGetValue("christmaslight", out var node) && node.NodeGroup is not null){
            var thisProt = MetaData(uid).EntityPrototype;
            foreach(var n in node.NodeGroup.Nodes)
            {
                if(TryComp<ChristmasLightsComponent>(n.Owner, out var jolly))
                {
                    if (MetaData(n.Owner).EntityPrototype != thisProt)
                        continue;
                    int i = jolly.modes.IndexOf(jolly.mode);
                    jolly.mode = jolly.modes[(i + 1) % jolly.modes.Count];
                    Dirty(jolly.Owner, jolly);
                }
            }
        }
    }
}


public sealed partial class SamePrototypeAdjacentNode : Node
{
    [ViewVariables]
    public string OwnerPrototypeID = default!;

    public override void Initialize(EntityUid owner, IEntityManager entMan)
    {
        base.Initialize(owner, entMan);
        

        if (String.IsNullOrEmpty(OwnerPrototypeID))
        {
            var prot = entMan.GetComponent<MetaDataComponent>(owner).EntityPrototype;
            DebugTools.Assert(prot is not null, "SamePrototypeAdjacentNode used on an entity with no EntityPrototype specified in metadata. Please reconsider your life choices that have lead you to this point.");
            OwnerPrototypeID = prot.ID;

        }
    }
    public override IEnumerable<Node> GetReachableNodes(TransformComponent xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        MapGridComponent? grid,
        IEntityManager entMan)
    {
        if (!xform.Anchored || grid == null)
            yield break;

        var gridIndex = grid.TileIndicesFor(xform.Coordinates);

        foreach (var (_, node) in NodeHelpers.GetCardinalNeighborNodes(nodeQuery, grid, gridIndex))
        {
            if (node is SamePrototypeAdjacentNode spaNode &&
                spaNode != this &&
                spaNode.OwnerPrototypeID == this.OwnerPrototypeID)
                yield return node;
        }
    }
}
