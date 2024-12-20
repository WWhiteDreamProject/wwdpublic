using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.NodeContainer;
using Content.Shared._White.Plumbing;
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

    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlot = default!;

    [Dependency] private readonly IGameTiming _timing = default!;


    //var simpleDemandQuery = EntityQueryEnumerator<PlumbingSimpleDemandComponent>();
    public override void Initialize()
    {

        SubscribeLocalEvent<PlumbingSimpleInputComponent, PlumbingPollAvailableInputsEvent>(OnInputsPoll);
        InitializeTank();
        //SubscribeLocalEvent<PlumbingSimpleInputComponent, PlumbingRequestOutputEvent>(OnFactoryRequest);
        //simpleDemandQuery = GetEntityQuery<PlumbingSimpleDemandComponent>();
    }

    public override void Update(float frameTime)
    {
        if((_timing.CurTick.Value % _timing.TickRate) == 0)
        {
            var query = EntityQueryEnumerator<PlumbingSimpleOutputComponent>();
            while(query.MoveNext(out EntityUid uid, out var comp))
            {
                ProcessOutputNode(uid, comp);
            }
        }
    }

    // todo: sync the nodes orientation with the client. Only need to send their direction over.
    //       A quick&dirty method could be catching MoveEvent and, if the owner rotated, send appearance data to the client.
    //       Will need PlumbingAppearanceComponent or something like that.
    //       I think it's mostly ready besides that. Only needs a reaction chamber and/or something to act as a filter.
    //       Also some minor touchups: proper pipe visuals (copy cablevisualisercomponent for that) and allow the pipes
    //       to be laid over plating.
    //       from the gameplay side: plumbing without infinite chemicals is as dull as physically possible.
    //       Add a factory that supports producing only 1u of a selected chem? It's very low, but it still allows
    //       for autonomous production of whatever you want. You can, of course, stack a bunch of them, but that
    //       will take a lot of space. On that note: prevent pipes from being laid under plumbing machinery, and vice versa.
    //       Current implementation relies too much on PlumbingSimple(Input/Output)Components. If i try to make an
    //       infinite factory, i'll have to make an PlumbingInfiniteTankComponent that creates a new 1u solution every time
    //       it catches the event. I dunno, it feels very meh.
    //       Don't know how to properly fix: processing reagent moving clientside so i don't get shitty prediction
    //       when a beaker i took out of an input port has 10u removed just a second later.
    //       This requires either client knowledge of nodes to get the connection data,
    //       or for the server to keep some PlumbingConnectionsComponent up-to-date every time the node network changes.
    //       The former will not work as nodes are in Content.Server.
    //       It is 7:25 AM. Fucking kill me.
    private void ProcessOutputNode(EntityUid uid, PlumbingSimpleOutputComponent comp)
    {
        if (!TryComp<NodeContainerComponent>(uid, out var nodecont))
            return;
        
        if (!nodecont.Nodes.TryGetValue("output", out var outputNode) || outputNode.NodeGroup is null)
            return;

        // get the tank we will drain reagents from
        // can be either an internal one or in an itemslot
        // or whatever the fuck is passed into the event object
        var tankev = new PlumbingGetTank();
        RaiseLocalEvent(uid, ref tankev);
        if (!tankev.solutionEnt.HasValue)
            return;

        Entity<SolutionComponent> sourceEnt = tankev.solutionEnt.Value;
        var senderSolution = sourceEnt.Comp.Solution;

        // check what nodes in the network are ready to receive our shit
        var ev = new PlumbingPollAvailableInputsEvent(senderSolution);
        foreach (var input in ((PlumbingNodeGroup) outputNode.NodeGroup).InputNodes)
        {
            RaiseLocalEvent(input.Owner, ref ev);
        }

        // the amount of reagents each of the input nodes in ev.Valid list are gonna get
        float fraction = Math.Min(comp.OutputPerSecond.Float(), senderSolution.Volume.Float()) / ev.Valid.Count;

        foreach ((var receiverEnt, List<string>? receiverFilters) in ev.Valid)
        {
            (var receiverUid, var receiverSolutionComp) = receiverEnt;
            Solution receiverSolution = receiverSolutionComp.Solution;
            var transferAmount = Math.Min(fraction, receiverSolution.AvailableVolume.Float()); // don't try to transfer more than we can fit

            Solution transfer;
            if (receiverFilters is null)
                transfer = senderSolution.SplitSolution(FixedPoint2.New(transferAmount));
            else
                transfer = senderSolution.SplitSolutionWithOnly(FixedPoint2.New(transferAmount), receiverFilters.ToArray() /*ffs*/ );

            receiverSolution.AddSolution(transfer, null);
            _solution.UpdateChemicals(receiverEnt);
            _solution.UpdateChemicals(sourceEnt);
        }
    }

    private void OnInputsPoll(EntityUid uid, PlumbingSimpleInputComponent comp, ref PlumbingPollAvailableInputsEvent args)
    {
        if (_solution.TryGetSolution(uid, comp.Solution, out var ent) && ent.Value.Comp.Solution.AvailableVolume > 0)
            args.Valid.Add((ent.Value, comp.UseFilters ? comp.ReagentFilters : null));
    }

    private void OnFactoryRequest(EntityUid uid, PlumbingFactoryComponent comp, PlumbingRequestOutputEvent args)
    {
        args.Solution.AddReagent(comp.reagentprototype, comp.perTick);
    }
}


[ByRefEvent]
public class PlumbingPollAvailableInputsEvent : EntityEventArgs
{
    public Solution Solution { get; private set; }
    public List<(Entity<SolutionComponent>, List<string>?)> Valid = new();
    public PlumbingPollAvailableInputsEvent(Solution solution)
    {
        Solution = solution;
    }
}

public class PlumbingRequestOutputEvent : EntityEventArgs
{
    public FixedPoint2 Quantity;
    public Solution Solution;
    public PlumbingRequestOutputEvent(FixedPoint2 quantity, Solution target)
    {
        Quantity = quantity;
        Solution = target;
    }
}

[ByRefEvent]
public class PlumbingGetTank : EntityEventArgs
{
    public Entity<SolutionComponent>? solutionEnt;
}
