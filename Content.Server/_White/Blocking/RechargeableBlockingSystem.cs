using Content.Server.Item;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.PowerCell.Components;

namespace Content.Server._White.Blocking;

public sealed class RechargeableBlockingSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RechargeableBlockingComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RechargeableBlockingComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RechargeableBlockingComponent, ItemToggleActivateAttemptEvent>(AttemptToggle);
        SubscribeLocalEvent<RechargeableBlockingComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<RechargeableBlockingComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    private void OnExamined(EntityUid uid, RechargeableBlockingComponent component, ExaminedEvent args)
    {
        BatteryComponent? batteryComponent = null;

        if (component.Discharged)
        {
            args.PushMarkup(Loc.GetString("rechargeable-blocking-discharged"));
            if (_battery.TryGetBatteryComponent(uid, out batteryComponent, out var batteryUid)
                && TryComp<BatterySelfRechargerComponent>(batteryUid, out var recharger)
                && recharger is { AutoRechargeRate: > 0, AutoRecharge: true })
            {
                var remainingTime = (int) (component.RechargeDelay - batteryComponent.CurrentCharge) / recharger.AutoRechargeRate;
                args.PushMarkup(Loc.GetString("rechargeable-blocking-remaining-time", ("remainingTime", remainingTime)));
            }
        }

        _powerCell.OnBatteryExamined(uid, batteryComponent, args);
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

    private void AttemptToggle(EntityUid uid, RechargeableBlockingComponent component, ref ItemToggleActivateAttemptEvent args)
    {
        if (!component.Discharged)
            return;

        _popup.PopupEntity(Loc.GetString("stunbaton-component-low-charge"), args.User ?? uid);
        args.Cancelled = true;
    }
    private void OnChargeChanged(EntityUid uid, RechargeableBlockingComponent component, ChargeChangedEvent args)
    {
        CheckCharge(uid, component);
    }

    private void OnPowerCellChanged(EntityUid uid, RechargeableBlockingComponent component, PowerCellChangedEvent args)
    {
        CheckCharge(uid, component);
    }

    private void CheckCharge(EntityUid uid, RechargeableBlockingComponent component)
    {
        if (!_battery.TryGetBatteryComponent(uid, out var battery, out _))
            return;

        if (battery.CurrentCharge < 1)
        {
            component.Discharged = true;
            _itemToggle.TryDeactivate(uid, predicted: false);
        }

        if (battery.CurrentCharge > component.RechargeDelay)
            component.Discharged = false;
    }
}
