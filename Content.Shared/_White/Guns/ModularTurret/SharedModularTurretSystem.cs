using Content.Shared._White.DollyMixture;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;

namespace Content.Shared._White.Guns.ModularTurret;

public abstract class SharedModularTurretSystem : EntitySystem
{
    [Dependency] private readonly SharedDollyMixtureSystem _dolly = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ModularTurretComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<ModularTurretComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ModularTurretComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<ModularTurretWeaponComponent, ShotAttemptedEvent>(OnModularTurretWeaponShotAttempt);
    }

    private void OnModularTurretWeaponShotAttempt(EntityUid turretUid, ModularTurretWeaponComponent comp, ref ShotAttemptedEvent args)
    {
        if (comp.OnlyUsableByTurret && !HasComp<ModularTurretComponent>(args.User))
            args.Cancel();
    }

    private void OnInsertAttempt(EntityUid uid, ModularTurretComponent comp, ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID != comp.Slot)
            return;

        if (!TryComp<ModularTurretWeaponComponent>(args.EntityUid, out var modweapon))
        {
            args.Cancel();
            return;
        }

        if (comp.MountClass is string turretClass &&
           !modweapon.WeaponClass.Contains(turretClass))
            args.Cancel();
    }

    protected virtual void OnInserted(EntityUid uid, ModularTurretComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == comp.Slot && TryComp<ModularTurretWeaponComponent>(args.Entity, out var weapon))
        {
            weapon.CurrentTurretHolder = uid;
            if (weapon.DollyMixRSIPath is string path)
                _dolly.Apply3D(uid, path);
        }
    }

    protected virtual void OnRemoved(EntityUid uid, ModularTurretComponent comp, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == comp.Slot && TryComp<ModularTurretWeaponComponent>(args.Entity, out var weapon))
        {
            weapon.CurrentTurretHolder = null;
            if (weapon.DollyMixRSIPath is not null)
                _dolly.Remove3D(uid);
        }
    }

}
