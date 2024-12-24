using Content.Server.Emp;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared._White.Misc.ChristmasLights;
using Content.Shared.ActionBlocker;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Nodes;

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

