using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared._White.Plumbing;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._White.Plumbing;

public sealed partial class PlumbingSystem : EntitySystem
{


    //var simpleDemandQuery = EntityQueryEnumerator<PlumbingSimpleDemandComponent>();
    public void InitializeVisualiser()
    {
        SubscribeLocalEvent<PlumbingInputOutputVisualiserComponent, NodeRotatedEvent>(OnNodeRotated);
    }

    private void OnNodeRotated(EntityUid uid, PlumbingInputOutputVisualiserComponent comp, NodeRotatedEvent args)
    {
        if(args.Node is PipeNode pipeNode)
        switch (pipeNode.Name)
        {
            case "input":
                comp.InputDir = pipeNode.CurrentPipeDirection.ToDirection();
                Dirty(uid, comp);
                return;
            case "output":
                comp.InputDir = pipeNode.CurrentPipeDirection.ToDirection();
                Dirty(uid, comp);
                return;
        }
    }
}
