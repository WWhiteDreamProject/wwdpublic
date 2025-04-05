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

    private void OnShutdown(Entity<DualWieldComponent> entity, ref ComponentShutdown args)
    {
        if (entity.Comp.LinkedWeapon is { } linked && TryComp<DualWieldComponent>(linked, out var linkedDual))
            linkedDual.LinkedWeapon = null;
    }

    private void OnGunShot(Entity<DualWieldComponent> entity, ref ShotAttemptedEvent args)
    {
        if (!_timing.IsFirstTimePredicted || args.Cancelled)
            return;

        var comp = entity.Comp;

        if (comp.LinkedWeapon is not { } linkedWeapon
            || !HasComp<DualWieldComponent>(linkedWeapon)
            || !TryComp<GunComponent>(entity, out var mainGun)
            || !TryComp<GunComponent>(linkedWeapon, out var linkedGun))
            return;

        if (_hands.GetActiveItem(args.User) != entity.Owner)
            return;

        if (mainGun.ShootCoordinates == null)
            return;

        // Synchronize targeting data at runtime to prevent issues later
        linkedGun.Target = mainGun.Target;
        var user = args.User;
        var shootCoordinates = mainGun.ShootCoordinates.Value;

        if (linkedGun.SelectedMode != SelectiveFire.FullAuto || mainGun.SelectedMode != SelectiveFire.FullAuto)
        {
            if (mainGun.ShotCounter >= 1)
                return;
        }

        Timer.Spawn(
            duration: TimeSpan.FromSeconds(comp.FireDelay),
            onFired: () =>
            {

                if (!Exists(linkedWeapon) || !Exists(user))
                    return;

                if (!TryComp<GunComponent>(linkedWeapon, out var currentLinkedGun))
                    return;

                if (_hands.GetActiveItem(user) != entity.Owner
                    || !_hands.IsHolding(user, linkedWeapon))
                    return;

                if (!TryComp<DualWieldComponent>(linkedWeapon, out var linkedDual)
                    || linkedDual.LinkedWeapon != entity.Owner
                    || !TryComp<DualWieldComponent>(entity.Owner, out var mainDual)
                    || mainDual.LinkedWeapon != linkedWeapon)
                    return;

                _gunSystem.AttemptShoot(user, linkedWeapon, currentLinkedGun, shootCoordinates);
            });

        var reloadDelta = linkedGun.CurrentAngleLastUpdate - mainGun.CurrentAngleLastUpdate;
        linkedGun.NextFire -= reloadDelta.Duration();
    }

    private void OnEquip(Entity<DualWieldComponent> entity, ref GotEquippedHandEvent args)
    {
        if (!TryComp<HandsComponent>(args.User, out var hands) || hands.Count != 2)
            return;

        foreach (var heldEntity in _hands.EnumerateHeld(args.User, hands))
        {
            if (heldEntity == entity.Owner || !TryComp<DualWieldComponent>(heldEntity, out var otherDualComp))
                continue;

            if (entity.Comp.LinkedWeapon != null || otherDualComp.LinkedWeapon != null)
                continue;

            entity.Comp.LinkedWeapon = heldEntity;
            otherDualComp.LinkedWeapon = entity.Owner;

            Dirty(heldEntity, otherDualComp);
            Dirty(entity, entity.Comp);

            _gunSystem.RefreshModifiers(entity.Owner);
            _gunSystem.RefreshModifiers(heldEntity);

            break;
        }
    }

    private void OnUnequip(Entity<DualWieldComponent> entity, ref GotUnequippedHandEvent args)
    {
        var comp = entity.Comp;
        if (comp.LinkedWeapon is not { } linked || !TryComp<DualWieldComponent>(linked, out var linkedDual))
            return;

        comp.LinkedWeapon = null;
        linkedDual.LinkedWeapon = null;

        _gunSystem.RefreshModifiers(linked);
        _gunSystem.RefreshModifiers(entity.Owner);

        Dirty(linked, linkedDual);
        Dirty(entity, comp);
    }

    private void OnRefreshModifiers(Entity<DualWieldComponent> entity, ref GunRefreshModifiersEvent args)
    {
        var comp = entity.Comp;
        if (comp.LinkedWeapon != null)
        {
            args.MaxAngle *= comp.SpreadMultiplier;
            args.MinAngle *= comp.SpreadMultiplier;
        }
    }
}
