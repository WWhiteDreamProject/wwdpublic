using Content.Shared._White;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._White;

public sealed class MoveEventProxyPassthroughSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public delegate void MoveEventHandlerProxy(ref MoveEventProxy ev);
    public event MoveEventHandlerProxy? OnGlobalMoveEvent;

    public override void Initialize()
    {
        _transform.OnGlobalMoveEvent += OnMoveEventGlobal;
    }

    private void OnMoveEventGlobal(ref MoveEvent ev)
    {
        var evproxy = new MoveEventProxy(ev.Entity, ev.OldPosition, ev.NewPosition, ev.OldRotation, ev.NewRotation);
        RaiseLocalEvent(ev.Sender, ref evproxy);
        OnGlobalMoveEvent?.Invoke(ref evproxy);
    }
}
