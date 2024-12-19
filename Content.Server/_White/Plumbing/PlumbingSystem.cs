using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.NodeContainer;
using Content.Shared._White.Plumbing;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._White.Plumbing;

public sealed class PlumbingSystem : EntitySystem
{

    [Dependency] private readonly SolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    //var simpleDemandQuery = EntityQueryEnumerator<PlumbingSimpleDemandComponent>();
    public override void Initialize()
    {
        SubscribeLocalEvent<PlumbingFactoryComponent, PlumbingPollOutputsEvent>(OnFactoryPoll);
        SubscribeLocalEvent < PlumbingFactoryComponent, PlumbingRequestOutputEvent>(OnFactoryRequest);
        //simpleDemandQuery = GetEntityQuery<PlumbingSimpleDemandComponent>();
    }

    public override void Update(float frameTime)
    {
        if((_timing.CurTick.Value % _timing.TickRate) == 0)
        {
            var query = EntityQueryEnumerator<PlumbingSimpleDemandComponent>();
            while(query.MoveNext(out EntityUid uid, out var comp))
            {
                SimpleDemandTick(uid, comp);
            }
        }
    }


    // todo: this is actually stupid
    // case in point: 3 tanks connected to one factory will all pull the same perTick of
    //                chemicals instead of everyone getting a third of the output.
    // move polls over to output nodes, so that they check for available inputs and not the other way around
    // it just makes more sense if i do not do the whole song and dance ss13 did in order to avoid shit like 10.0000001 water
    // i can either do a "faithful recreation", or do something that resembles the original in basic functionality.
    // it's easier to do the latter, especially if we're leaning on the whole "different from ss13" idea.
    private void SimpleDemandTick(EntityUid uid, PlumbingSimpleDemandComponent comp)
    {
        if (!TryComp<NodeContainerComponent>(uid, out var nodecont))
            return;

        if (!nodecont.Nodes.TryGetValue("input", out var inputNode))
            return;

        if (!TryComp<ContainerManagerComponent>(uid, out var contman) || ! _solution.TryGetSolution(uid, comp.Solution, out var _, out var solution))
            return;

        var ev = new PlumbingPollOutputsEvent();
        foreach(var output in ((PlumbingNodeGroup) inputNode.NodeGroup!).OutputNodes)
        {
            RaiseLocalEvent(output.Owner, ref ev);
        }
        var reqev = new PlumbingRequestOutputEvent(comp.RequestPerTick / ev.Valid.Count, solution);
        foreach (var valid in ev.Valid)
        {
            RaiseLocalEvent(valid, reqev);
        }


        // trycomp nodecontainer
        // if present, try get input node
        // if present, get nodegroup
        // create an instance of an event
        // foreach node in nodegroup
        // raise event by ref at node
        //
        // implement tg logic for sucking in shit
        // equal parts from all providers; round robin per provider
        //      or
        // fuck it
    }

    private void OnFactoryPoll(EntityUid uid, PlumbingFactoryComponent comp, ref PlumbingPollOutputsEvent args)
    {
        args.Valid.Add(uid);
    }

    private void OnFactoryRequest(EntityUid uid, PlumbingFactoryComponent comp, PlumbingRequestOutputEvent args)
    {
        args.Solution.AddReagent(comp.reagentprototype, comp.perTick);
    }

}


[ByRefEvent]
public class PlumbingPollOutputsEvent : EntityEventArgs
{
    public string? ReagentFilter { get; private set; }
    public List<EntityUid> Valid = new();
    public PlumbingPollOutputsEvent(string? filter = null)
    {
        ReagentFilter = filter;
    }
}

public class PlumbingRequestOutputEvent : EntityEventArgs {
    public FixedPoint2 Quantity;
    public Solution Solution;
    public PlumbingRequestOutputEvent(FixedPoint2 quantity, Solution target)
    {
        Quantity = quantity;
        Solution = target;
    }
}
