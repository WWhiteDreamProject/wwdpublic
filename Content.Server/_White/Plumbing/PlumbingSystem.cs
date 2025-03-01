using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.NodeContainer;
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
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        SubscribeLocalEvent<PlumbingInputOutputVisualiserComponent, ComponentInit>(OnInputOutputVisualiserInit);
        
        SubscribeLocalEvent<PlumbingSimpleInputComponent, PlumbingPollAvailableInputsEvent>(OnInputsPoll);
        InitializeTank();
        InitializeSeparator();
        InitializeVisualiser();
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
                if(comp.Enabled)
                    ProcessOutputNode(uid, comp);
            }
        }
    }
    /// <summary>
    /// this is a mess, but 95% of this mess is just sanity checks, because mine is long gone.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private bool TryGetPipeNode(EntityUid uid, string type, [NotNullWhen(true)] out PipeNode? pnode)
    {
        pnode = null;
        // it is not fine if a plumbing machinery misses the node container: the whole plumbing thing operates on nodes.
        DebugTools.Assert(TryComp<NodeContainerComponent>(uid, out var nodeCont), "No NodeContainerComponent present on a plumbing machinery. There is only 0.1% chance this is intended");

        // it is fine if we're missing out node: we may be called on an input-only or output-only machinery, they will lack an output/input port respectively.
        if (!nodeCont.Nodes.TryGetValue(type, out var node))
            return false;

        // it is definitely not fine if we get some node type other than PipeNode. // low-priority todo: implement an actual PlumbingNode so i don't have to hijack PipeNode. // Counterpoint: pipenode does pretty much everything i need // Counnter-counterpoint: working around PipeDirection is kinda fucking annoying when i actually need Direction.
        DebugTools.Assert(node is PipeNode, $"Expected PipeNode node, got {node.GetType()}.");
        pnode = (PipeNode) node;
        if (pnode.CurrentPipeDirection != PipeDirection.None)
        {
            pnode.CurrentPipeDirection.ToDirection(); // throws ArgumentOutOfRangeException if pipeDirection isn't a cardinal direction. Here's to hoping it stays that way.
        }
        return true;
    }
    private void OnInputOutputVisualiserInit(EntityUid uid, PlumbingInputOutputVisualiserComponent comp, ComponentInit args)
    {
        bool set = false;
        if (TryGetPipeNode(uid, "input", out var inputNode))
        {
            comp.InputDir = inputNode.CurrentPipeDirection.ToDirection(); // it sucks that it's not the other way around, yeah
            set = true;
        }

        if (TryGetPipeNode(uid, "output", out var outputNode))
        {
            comp.OutputDir = outputNode.CurrentPipeDirection.ToDirection();
            set = true;
        }
        // plumbing machinery should have either an input port, an output port or both. 
        DebugTools.Assert(set, "PlumbingInputOutputVisualiserComponent added to entity with no input or output nodes.");
        Dirty(uid, comp);
    }



    // todo: a reaction chamber and/or something to act as a filter.
    //
    //       prevent pipes from being laid under plumbing machinery, and vice versa.
    //
    //       reevaluate my life choices
    //
    //       get rid of inserting in input ports, it introduces stupid prediction bullshit. Stick to pouring in and out.
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
        //if (!tankev.solutionEnt.HasValue)
        //    return;

        // yeah, this is way too convoluted for what i am trying to do here.
        Solution? senderSolution;
        Entity<SolutionComponent>? sourceEnt = null;
        if (tankev.solutionEnt.HasValue)
        {
            sourceEnt = tankev.solutionEnt; // we either use the solution provided in the Entity<SolutionComponent> (means that the solution is attached to an entity, duh)
            senderSolution = sourceEnt.Value.Comp.Solution;
        }
        else
        {
            senderSolution = tankev.solution; // or we use a plain Solution object, in case it was just created or something.
        }// this is important because we need to call _solution.UpdateChemicals(Entity<SolutionComponent>) if we change the entity's solution
        
        if (senderSolution is null)
            return; // if it's null, then we have neither an entity nor a standalone Solution. Probably should also log this: we should at least get an empty solution.

        if (senderSolution.AvailableVolume == 0)
            return;

        // check what nodes in the network are ready to receive our shit
        var ev = new PlumbingPollAvailableInputsEvent(senderSolution);
        foreach (var input in ((PlumbingNodeGroup) outputNode.NodeGroup).InputNodes)
        {
            RaiseLocalEvent(input.Owner, ref ev);
        }

        // the amount of reagents each of the input nodes in ev.Valid list are gonna get
        float fraction = Math.Min(comp.OutputPerSecond.Float(), senderSolution.Volume.Float()) / ev.Valid.Count;

        foreach ((var receiverEnt, bool isWhitelist ,List<string> receiverFilters) in ev.Valid)
        {
            (var receiverUid, var receiverSolutionComp) = receiverEnt;
            Solution receiverSolution = receiverSolutionComp.Solution;
            var transferAmount = Math.Min(fraction, receiverSolution.AvailableVolume.Float()); // don't try to transfer more than we can fit

            Solution transfer;
            if (isWhitelist)
                transfer = senderSolution.SplitSolutionWithOnly(FixedPoint2.New(transferAmount), receiverFilters.ToArray() /*ffs*/ );
            else
                transfer = senderSolution.SplitSolutionWithout(FixedPoint2.New(transferAmount), receiverFilters.ToArray() /*ffs*/ );

            _solution.AddSolution(receiverEnt, transfer);
            if(sourceEnt.HasValue)
                _solution.UpdateChemicals(sourceEnt.Value);
        }
    }

    private void OnInputsPoll(EntityUid uid, PlumbingSimpleInputComponent comp, ref PlumbingPollAvailableInputsEvent args)
    {
        if (_solution.TryGetSolution(uid, comp.Solution, out var ent) && ent.Value.Comp.Solution.AvailableVolume > 0)
            args.Valid.Add((ent.Value, comp.Whitelist, comp.ReagentFilters));
        
    }
}


[ByRefEvent]
public class PlumbingPollAvailableInputsEvent : EntityEventArgs
{
    public Solution Solution { get; private set; }
    public List<(Entity<SolutionComponent>, bool, List<string>)> Valid = new();
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
    /// <summary>
    /// Entity which will be used as a tank.
    /// </summary>
    public Entity<SolutionComponent>? solutionEnt;
    /// <summary>
    /// solutionEnt is prioritized over solution.
    /// </summary>
    public Solution? solution;
}


//[DataDefinition]
//public sealed partial class PlumbingFilterEntry
//{
//    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
//    public string ReagentPrototype = default!;
//    [DataField(required: true)]
//    public FixedPoint2 MaxQuantity;
//}
