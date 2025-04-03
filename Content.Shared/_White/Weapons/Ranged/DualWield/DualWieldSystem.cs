using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._White.Weapons.Ranged.DualWield;

public sealed class DualWieldSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DualWieldComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DualWieldComponent, ShotAttemptedEvent>(OnGunShot);
        SubscribeLocalEvent<DualWieldComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<DualWieldComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<DualWieldComponent, GunRefreshModifiersEvent>(OnRefreshModifiers);
    }

    private void OnShutdown(Entity<DualWieldComponent> dualWieldEntity, ref ComponentShutdown args)
    {
        if (dualWieldEntity.Comp.LinkedWeapon is { } linked &&
            TryComp<DualWieldComponent>(linked, out var dual))
            dual.LinkedWeapon = null;
    }

    private void OnGunShot(Entity<DualWieldComponent> dualWieldEntity, ref ShotAttemptedEvent args)
    {
        if(!_timing.IsFirstTimePredicted || args.Cancelled)
            return;

        var comp = dualWieldEntity.Comp;

        if (comp.LinkedWeapon == null ||
            !HasComp<DualWieldComponent>(dualWieldEntity.Comp.LinkedWeapon) ||
            !TryComp<GunComponent>(dualWieldEntity, out var mainGun) ||
            !TryComp<GunComponent>(comp.LinkedWeapon.Value, out var linkedGun))
            return;

        if (_hands.GetActiveItem(args.User) != dualWieldEntity)
            return;

        if (mainGun.ShootCoordinates == null)
            return;

        linkedGun.Target = mainGun.Target; // Lock the values at runtime to prevent issues later
        var user = args.User;
        var linkedGunUid = comp.LinkedWeapon.Value;
        var shootCoordinates = mainGun.ShootCoordinates.Value;

        Timer.Spawn(
            duration: TimeSpan.FromSeconds(comp.FireDelay),
            onFired: () =>
            {
                if (!Exists(linkedGunUid))
                    return;

                // Fire the linked weapon with same coordinates
                _gunSystem.AttemptShoot(
                    user: user,
                    gunUid: linkedGunUid,
                    gun: linkedGun,
                    toCoordinates: shootCoordinates);
            });
    }

    private void OnEquip(Entity<DualWieldComponent> dualWieldEntity, ref GotEquippedHandEvent args)
    {
        var comp = dualWieldEntity.Comp;

        if (!TryComp<HandsComponent>(args.User, out var hands))
            return;

        if (hands.Count != 2)
            return;

        foreach (var held in _hands.EnumerateHeld(args.User, hands))
        {
            if (held == dualWieldEntity.Owner || !TryComp<DualWieldComponent>(held, out var dual))
                continue;

            if (dualWieldEntity.Comp.LinkedWeapon != null || dual.LinkedWeapon != null)
                continue;

            comp.LinkedWeapon = held;
            dual.LinkedWeapon = dualWieldEntity.Owner;

            Dirty(held, dual);
            Dirty(dualWieldEntity.Owner, comp);

            _gunSystem.RefreshModifiers(dualWieldEntity.Owner);
            _gunSystem.RefreshModifiers(held);

            break;
        }
    }

    private void OnUnequip(Entity<DualWieldComponent> dualWieldEntity, ref GotUnequippedHandEvent args)
    {
        var comp = dualWieldEntity.Comp;

        if (comp.LinkedWeapon is not { } linked || !TryComp<DualWieldComponent>(linked, out var dual))
            return;

        var tempLinked = comp.LinkedWeapon.Value;

        comp.LinkedWeapon = null;
        dual.LinkedWeapon = null;

        _gunSystem.RefreshModifiers(tempLinked);
        _gunSystem.RefreshModifiers(dualWieldEntity.Owner);

        Dirty(tempLinked, dual);
        Dirty(dualWieldEntity, comp);
    }

    private void OnRefreshModifiers(Entity<DualWieldComponent> dualWieldEntity, ref GunRefreshModifiersEvent args)
    {
        var comp = dualWieldEntity.Comp;

        if (comp.LinkedWeapon == null)
            return;

        args.MaxAngle *= comp.SpreadMultiplier;
        args.MinAngle *= comp.SpreadMultiplier;
    }
}
