using Content.Server.Stack;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    [Dependency] private readonly StackSystem _stack = default!; // WD EDIT
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!; // WWDP

    protected override void Cycle(EntityUid uid, BallisticAmmoProviderComponent component, MapCoordinates coordinates, GunComponent? gunComponent)
    {
        EntityUid? ent = null;
        if (!Resolve(uid, ref gunComponent, false))
            return;

        // TODO: Combine with TakeAmmo
        if (component.Entities.Count > 0)
        {
            var existing = component.Entities[^1];
            component.Entities.RemoveAt(component.Entities.Count - 1);

            Containers.Remove(existing, component.Container);
            EnsureShootable(existing);
            EjectCartridge(existing, gunComp: gunComponent);
        }
        else if (component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            ent = Spawn(component.Proto, coordinates);
            EnsureShootable(ent.Value);
        }

        if (ent != null)
            EjectCartridge(ent.Value, gunComp: gunComponent);

        var cycledEvent = new GunCycledEvent();
        RaiseLocalEvent(uid, ref cycledEvent);
    }

    // WWDP extract round
    protected override void Extract(EntityUid uid, MapCoordinates coordinates, BallisticAmmoProviderComponent component,
        EntityUid user)
    {
        EntityUid entity;

        if (component.Entities.Count > 0)
        {
            entity = component.Entities[^1];
            component.Entities.RemoveAt(component.Entities.Count - 1);
            EnsureShootable(entity);
        }
        else if (component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            entity = Spawn(component.Proto, coordinates);
            EnsureShootable(entity);
        }
        else
        {
            Popup(Loc.GetString("gun-ballistic-empty"), uid, user);
            return;
        }

        _handsSystem.PickupOrDrop(user, entity);
    }
    // WWDP extract round end

    // WD EDIT START
    protected override EntityUid GetStackEntity(EntityUid uid, StackComponent stack)
    {
        return _stack.Split(uid, 1, Transform(uid).Coordinates, stack) ?? uid;
    }
    // WD EDIT END
}
