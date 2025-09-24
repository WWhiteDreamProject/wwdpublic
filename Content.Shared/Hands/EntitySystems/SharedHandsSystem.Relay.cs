using Content.Shared._White.Move;
using Content.Shared.Camera;
using Content.Shared.Hands.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<HandsComponent, GetEyeOffsetRelayedEvent>(RelayEvent);
        SubscribeLocalEvent<HandsComponent, GetEyePvsScaleRelayedEvent>(RelayEvent);
        SubscribeLocalEvent<HandsComponent, RefreshMovementSpeedModifiersEvent>(RelayEvent);
        SubscribeLocalEvent<HandsComponent, MoveEventProxy>(RelayEvent);
    }

    private void RelayEvent<T>(Entity<HandsComponent> entity, ref T args) where T : notnull // WD EDIT
    {
        var ev = new HeldRelayedEvent<T>(args);
        foreach (var held in EnumerateHeld(entity, entity.Comp))
        {
            RaiseLocalEvent(held, ref ev);
        }
    }
}
