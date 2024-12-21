using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Events;
using Content.Server.Stunnable.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Stunnable;
using Content.Shared.Stunnable.Events;


namespace Content.Server.Stunnable.Systems
{
    public sealed class StunbatonSystem : SharedStunbatonSystem
    {
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly RiggableSystem _riggableSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly BatterySystem _battery = default!;
        [Dependency] private readonly SharedItemToggleSystem _itemToggle = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StunbatonComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<StunbatonComponent, SolutionContainerChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<StunbatonComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
            SubscribeLocalEvent<StunbatonComponent, ItemToggleActivateAttemptEvent>(TryTurnOn);
            SubscribeLocalEvent<StunbatonComponent, ItemToggledEvent>(ToggleDone);
            SubscribeLocalEvent<StunbatonComponent, ChargeChangedEvent>(OnChargeChanged);
            SubscribeLocalEvent<StunbatonComponent, PowerCellChangedEvent>(OnPowerCellChanged); // WD EDIT
            SubscribeLocalEvent<StunbatonComponent, KnockdownOnHitAttemptEvent>(OnKnockdownHitAttempt); // WD EDIT
        }

        private void OnStaminaHitAttempt(Entity<StunbatonComponent> entity, ref StaminaDamageOnHitAttemptEvent args)
        {
            // WD EDIT START
            if (!_itemToggle.IsActivated(entity.Owner) || !TryUseCharge(entity))
                args.Cancelled = true;
            // WD EDIT END
        }

        // WD EDIT START
        private void OnKnockdownHitAttempt(Entity<StunbatonComponent> entity, ref KnockdownOnHitAttemptEvent args)
        {
            if (!_itemToggle.IsActivated(entity.Owner) || !TryUseCharge(entity))
                args.Cancelled = true;
        }
        // WD EDIT END

        private void OnExamined(Entity<StunbatonComponent> entity, ref ExaminedEvent args)
        {
            var onMsg = _itemToggle.IsActivated(entity.Owner)
            ? Loc.GetString("comp-stunbaton-examined-on")
            : Loc.GetString("comp-stunbaton-examined-off");
            args.PushMarkup(onMsg);

            if (_battery.TryGetBatteryComponent(entity, out var battery, out _)) // WD EDIT
            {
                var count = (int) (battery.CurrentCharge / entity.Comp.EnergyPerUse);
                args.PushMarkup(Loc.GetString("melee-battery-examine", ("color", "yellow"), ("count", count)));
            }
        }

        private void ToggleDone(Entity<StunbatonComponent> entity, ref ItemToggledEvent args)
        {
            _item.SetHeldPrefix(entity.Owner, args.Activated ? "on" : "off");
        }

        private void TryTurnOn(Entity<StunbatonComponent> entity, ref ItemToggleActivateAttemptEvent args)
        {
            // WD EDIT START
            if (!_battery.TryGetBatteryComponent(entity, out var battery, out _)
                || battery.CurrentCharge < entity.Comp.EnergyPerUse)
            {

                args.Cancelled = true;

                if (args.User != null)
                {
                    _popup.PopupEntity(Loc.GetString("stunbaton-component-low-charge"), (EntityUid) args.User,
                        (EntityUid) args.User);
                }

                return;
            }
            // WD EDIT END

            if (TryComp<RiggableComponent>(entity, out var rig) && rig.IsRigged)
            {
                _riggableSystem.Explode(entity.Owner, battery, args.User);
            }
        }

        // https://github.com/space-wizards/space-station-14/pull/17288#discussion_r1241213341
        private void OnSolutionChange(Entity<StunbatonComponent> entity, ref SolutionContainerChangedEvent args)
        {
            // Explode if baton is activated and rigged.
            if (!TryComp<RiggableComponent>(entity, out var riggable) ||
                !TryComp<BatteryComponent>(entity, out var battery))
                return;

            if (_itemToggle.IsActivated(entity.Owner) && riggable.IsRigged)
                _riggableSystem.Explode(entity.Owner, battery);
        }

        private void SendPowerPulse(EntityUid target, EntityUid? user, EntityUid used)
        {
            RaiseLocalEvent(target, new PowerPulseEvent()
            {
                Used = used,
                User = user
            });
        }

        private void OnChargeChanged(Entity<StunbatonComponent> entity, ref ChargeChangedEvent args)
        {
            CheckCharge(entity); // WD EDIT
        }

        // WD EDIT START
        private void OnPowerCellChanged(Entity<StunbatonComponent> entity, ref PowerCellChangedEvent args)
        {
            CheckCharge(entity);
        }

        private void CheckCharge(Entity<StunbatonComponent> entity)
        {
            if (!_battery.TryGetBatteryComponent(entity, out var battery, out _)
                || battery.CurrentCharge < entity.Comp.EnergyPerUse)
                _itemToggle.TryDeactivate(entity.Owner, predicted: false);
        }

        private bool TryUseCharge(Entity<StunbatonComponent> entity)
        {
            return _battery.TryGetBatteryComponent(entity, out var battery, out var batteryUid)
                && battery.CurrentCharge >= entity.Comp.EnergyPerUse
                && _battery.TryUseCharge(batteryUid.Value, entity.Comp.EnergyPerUse / 2, battery);
        }
        // WD EDIT END
    }
}
