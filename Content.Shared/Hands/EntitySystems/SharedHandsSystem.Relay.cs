using Content.Shared._White;
using Content.Shared.Hands.Components;
using Content.Shared.MouseRotator;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<HandsComponent, RefreshMovementSpeedModifiersEvent>(RelayEvent);
        SubscribeLocalEvent<HandsComponent, MoveEventProxy>(RelayMoveEvent); // WWDP EDIT
    }

    private void RelayEvent<T>(Entity<HandsComponent> entity, ref T args) where T : EntityEventArgs
    {
        var ev = new HeldRelayedEvent<T>(args);
        foreach (var held in EnumerateHeld(entity, entity.Comp))
        {
            RaiseLocalEvent(held, ref ev);
        }
    }

    //WWDP EDIT START
    private void RelayMoveEvent(EntityUid uid, HandsComponent comp, ref MoveEventProxy args)
    {
        var ev = new HolderMoveEvent(args);
        foreach (var itemUid in EnumerateHeld(uid, comp))
        {
            RaiseLocalEvent(itemUid, ref ev);
        }
    }

    //WWDP EDIT END
}

// WWDP STUFF I GUESS
[ByRefEvent]
public readonly struct HolderMoveEvent(MoveEventProxy ev)
{
    public readonly MoveEventProxy Ev = ev;
}
