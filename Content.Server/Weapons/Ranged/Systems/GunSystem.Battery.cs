using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared._White.Guns;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.FixedPoint;
using Content.Shared.PowerCell.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    [Dependency] private readonly TemperatureSystem _temp = default!; // WWDP EDIT

    protected override void InitializeBattery()
    {
        base.InitializeBattery();

        // Hitscan
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, DamageExamineEvent>(OnBatteryDamageExamine);

        // Projectile
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, DamageExamineEvent>(OnBatteryDamageExamine);
        //// WWDP EDIT START
        SubscribeLocalEvent<ProjectileContainerBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<ProjectileContainerBatteryAmmoProviderComponent, EntGotInsertedIntoContainerMessage>(OnContainerBatteryInserted);
        SubscribeLocalEvent<ProjectileContainerBatteryAmmoProviderComponent, EntGotRemovedFromContainerMessage>(OnContainerBatteryRemoved);
        SubscribeLocalEvent<ProjectileContainerBatteryAmmoProviderComponent, DamageExamineEvent>(OnBatteryDamageExamine);
        SubscribeLocalEvent<ContainerBatteryAmmoTrackerComponent, ChargeChangedEvent>(OnBatteryChargeChangeTracker);

        SubscribeLocalEvent<HitscanContainerBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<HitscanContainerBatteryAmmoProviderComponent, EntGotInsertedIntoContainerMessage>(OnContainerBatteryInserted);
        SubscribeLocalEvent<HitscanContainerBatteryAmmoProviderComponent, EntGotRemovedFromContainerMessage>(OnContainerBatteryRemoved);
        SubscribeLocalEvent<HitscanContainerBatteryAmmoProviderComponent, DamageExamineEvent>(OnBatteryDamageExamine);
        //// WWDP EDIT END
    }

    private void OnBatteryStartup(EntityUid uid, BatteryAmmoProviderComponent component, ComponentStartup args)
    {
        UpdateShots(uid, component);
    }

    private void OnBatteryChargeChange(EntityUid uid, BatteryAmmoProviderComponent component, ref ChargeChangedEvent args)
    {
        UpdateShots(uid, component, args.Charge, args.MaxCharge);
    }

    public void UpdateShots(EntityUid uid, BatteryAmmoProviderComponent component) // WWDP EDIT - private -> public
    {
        var batteryUid = component is ContainerBatteryAmmoProviderComponent ? Transform(uid).ParentUid : uid; // WWDP EDIT
        if (!TryComp<BatteryComponent>(batteryUid, out var battery))    // WWDP EDIT
            return;

        UpdateShots(uid, component, battery.CurrentCharge, battery.MaxCharge);
    }

    private void UpdateShots(EntityUid uid, BatteryAmmoProviderComponent component, float charge, float maxCharge)
    {
        var shots = (int) (charge / component.FireCost);
        var maxShots = (int) (maxCharge / component.FireCost);

        if (component.Shots != shots || component.Capacity != maxShots)
        {
            Dirty(uid, component);
        }

        component.Shots = shots;
        component.Capacity = maxShots;
        UpdateBatteryAppearance(uid, component);
    }

    private void OnBatteryDamageExamine(EntityUid uid, BatteryAmmoProviderComponent component, ref DamageExamineEvent args)
    {
        var damageSpec = GetDamage(component);

        if (damageSpec == null)
            return;

        var damageType = component switch
        {
            HitscanBatteryAmmoProviderComponent => Loc.GetString("damage-hitscan"),
            ProjectileBatteryAmmoProviderComponent => Loc.GetString("damage-projectile"),
            _ => throw new ArgumentOutOfRangeException(),
        };

        _damageExamine.AddDamageExamine(args.Message, damageSpec, damageType);
    }

    private DamageSpecifier? GetDamage(BatteryAmmoProviderComponent component)
    {
        if (component is ProjectileBatteryAmmoProviderComponent battery)
        {
            if (ProtoManager.Index<EntityPrototype>(battery.Prototype).Components
                .TryGetValue(_factory.GetComponentName(typeof(ProjectileComponent)), out var projectile))
            {
                var p = (ProjectileComponent) projectile.Component;

                if (!p.Damage.Empty)
                {
                    return p.Damage;
                }
            }

            return null;
        }

        if (component is HitscanBatteryAmmoProviderComponent hitscan)
        {
            return ProtoManager.Index<HitscanPrototype>(hitscan.Prototype).Damage;
        }

        return null;
    }

    protected override void TakeCharge(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        if(component is ContainerBatteryAmmoProviderComponent comp)
        {
            uid = comp.Linked!.Value; // the validity of this should be enforced by EntGotInserted/Removed event handlers below.
        }
        // Will raise ChargeChangedEvent
        _battery.UseCharge(uid, component.FireCost);
    }

    // WWDP EDIT START
    private void OnContainerBatteryInserted(EntityUid uid, ContainerBatteryAmmoProviderComponent comp, EntGotInsertedIntoContainerMessage args)
    {
        var contUid = args.Container.Owner;
        var tracker = EnsureComp<ContainerBatteryAmmoTrackerComponent>(contUid);
        tracker.Linked.Add(uid);
        comp.Linked = contUid;
        UpdateShots(uid, comp);
    }

    private void OnContainerBatteryRemoved(EntityUid uid, ContainerBatteryAmmoProviderComponent comp, EntGotRemovedFromContainerMessage args)
    {
        var contUid = args.Container.Owner;
        if (!TryComp<ContainerBatteryAmmoTrackerComponent>(contUid, out var tracker))
            return;
        tracker.Linked.Remove(uid);
        comp.Linked = null;
        if (tracker.Linked.Count == 0)
            RemComp(contUid, tracker);
        UpdateShots(uid, comp, 0, 0);
    }

    private void OnBatteryChargeChangeTracker(EntityUid uid, ContainerBatteryAmmoTrackerComponent comp, ref ChargeChangedEvent args)
    {
        foreach (var gunUid in comp.Linked)
        {
            if(TryComp<ProjectileContainerBatteryAmmoProviderComponent>(gunUid, out var projectileAmmoProvider))
                UpdateShots(gunUid, projectileAmmoProvider, args.Charge, args.MaxCharge);
            else if(TryComp<HitscanContainerBatteryAmmoProviderComponent>(gunUid, out var hitscanAmmoProvider))
                UpdateShots(gunUid, hitscanAmmoProvider, args.Charge, args.MaxCharge);
        }
    }
    // WWDP EDIT END
}
