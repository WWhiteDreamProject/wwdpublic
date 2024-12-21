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


// i am about to write ui again.
// I am afraid God can't help me even if he wanted to.
public sealed partial class PlumbingSystem
{   public void InitializeSeparator()
    {

        SubscribeLocalEvent<PlumbingInternalTankComponent, PlumbingGetTank>(OnGetTankInternal);
        SubscribeLocalEvent<PlumbingItemSlotTankComponent, PlumbingGetTank>(OnGetTankItemSlot);
    }


}
