using Content.Shared._White.Plumbing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._White.Plumbing;

public sealed class PlumbingSystem : EntitySystem
{
    public override void Initialize()
    {
        //SubscribeLocalEvent<PlumbingSimpleDemandComponent, PlumbingTickEvent>(SimpleDemandTick);
    }

    public override void Update(float frameTime)
    {

    }

    private void SimpleDemandTick(EntityUid uid, PlumbingSimpleDemandComponent comp)
    {
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
}


[ByRef]
public class PlumbingPollOutputsEvent : EntityEventArgs
{
    public string? ReagentFilter { get; private set; }
    public List<EntityUid> valid = new();
    public PlumbingPollOutputsEvent(string? filter = null)
    {
        ReagentFilter = filter;
    }
}

