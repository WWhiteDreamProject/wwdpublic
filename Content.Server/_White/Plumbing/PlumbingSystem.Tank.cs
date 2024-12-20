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


    //var simpleDemandQuery = EntityQueryEnumerator<PlumbingSimpleDemandComponent>();
    public void InitializeTank()
    {

        SubscribeLocalEvent<PlumbingInternalTankComponent, PlumbingGetTank>(OnGetTankInternal);
        SubscribeLocalEvent<PlumbingItemSlotTankComponent, PlumbingGetTank>(OnGetTankItemSlot);

        //SubscribeLocalEvent<PlumbingSimpleInputComponent, PlumbingRequestOutputEvent>(OnFactoryRequest);
        //simpleDemandQuery = GetEntityQuery<PlumbingSimpleDemandComponent>();
    }

    private void OnGetTankInternal(EntityUid uid, PlumbingInternalTankComponent comp, ref PlumbingGetTank args)
    {
        if (_solution.TryGetSolution(uid, comp.Solution, out var ent))
            args.solutionEnt = ent;
    }
    private void OnGetTankItemSlot(EntityUid uid, PlumbingItemSlotTankComponent comp, ref PlumbingGetTank args)
    {
        if (_itemSlot.TryGetSlot(uid, comp.Slot, out var slot) &&
            slot.HasItem &&
            _solution.TryGetRefillableSolution(slot.Item!.Value, out var ent, out var _)) // if you can easily refill a container, you should be able to empty it just as easily
            args.solutionEnt = ent;
    }

}
