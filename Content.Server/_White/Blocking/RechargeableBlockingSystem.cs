using Content.Server.Item;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.PowerCell.Components;

namespace Content.Server._White.Blocking;

public sealed class RechargeableBlockingSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RechargeableBlockingComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RechargeableBlockingComponent, ItemToggleActivateAttemptEvent>(AttemptToggle);
        SubscribeLocalEvent<RechargeableBlockingComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<RechargeableBlockingComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    private void OnDamageChanged(EntityUid uid, RechargeableBlockingComponent component, DamageChangedEvent args)
    {
        if (!_battery.TryGetBatteryComponent(uid, out var batteryComponent, out var batteryUid)
            || !_itemToggle.IsActivated(uid)
            || args.DamageDelta == null)
            return;

        var batteryUse = Math.Min(args.DamageDelta.GetTotal().Float(), batteryComponent.CurrentCharge);
        _battery.TryUseCharge(batteryUid.Value, batteryUse, batteryComponent);
    }

    public void AttemptToggle(EntityUid uid, RechargeableBlockingComponent component, ItemToggleActivateAttemptEvent args)
    {
        if (!_battery.TryGetBatteryComponent(uid, out var battery, out _)
            || battery.CurrentCharge > component.RechargeDelay)
            return;

        args.Cancelled = true;
    }
    private void OnChargeChanged(EntityUid uid, RechargeableBlockingComponent component, ChargeChangedEvent args)
    {
        CheckCharge(uid);
    }

    private void OnPowerCellChanged(EntityUid uid, RechargeableBlockingComponent component, PowerCellChangedEvent args)
    {
        CheckCharge(uid);
    }

    private void CheckCharge(EntityUid uid)
    {
        if (!_battery.TryGetBatteryComponent(uid, out var battery, out _)
            || battery.CurrentCharge > 1)
            return;

        _itemToggle.TryDeactivate(uid, predicted: false);
    }
}
