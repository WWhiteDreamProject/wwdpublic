using Content.Shared.Hands.EntitySystems;
using Content.Shared._White.RenderOrderSystem;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.RenderOrderSystem;

// TODO:
// Currently any clientside change to the render order can be invalidated by a
// RenderOrderComponent state update, even if the state update doesn't set a new value
// Probably will have to keep a separate list of clientside changes, but that's for later,
// there isn't many features that could yield problems with that, so it's low priority for now.
// I just want to get the sidestream done and go sleep.
public sealed class RenderOrderSystem : SharedRenderOrderSystem
{
    public readonly uint DefaultRenderOrder = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RenderOrderComponent, AfterAutoHandleStateEvent>((uid, comp, args) => UpdateRenderOrder(uid, comp));
    }


    protected override void UpdateRenderOrder(EntityUid uid, RenderOrderComponent comp)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        DebugTools.Assert(comp.ValueOrder.Count == comp.Values.Count, "comp.Values and comp.ValueOrder have different entry counts");
        if(comp.ValueOrder.Count == 0)
        {
            sprite.RenderOrder = DefaultRenderOrder;
            return;
        }
        sprite.RenderOrder = comp.Values[comp.ValueOrder.Last()];
    }
}


