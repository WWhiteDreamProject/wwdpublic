using System.Diagnostics.CodeAnalysis;
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
using Robust.Shared.Containers;

namespace Content.Server.Stunnable.Systems
{
    public sealed class StunbatonSystem : SharedStunbatonSystem
    {
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly RiggableSystem _riggableSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly BatterySystem _battery = default!;
        [Dependency] private readonly SharedItemToggleSystem _itemToggle = default!;
        [Dependency] private readonly SharedContainerSystem _containers = default!; // WD EDIT

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
        }

        private void OnStaminaHitAttempt(Entity<StunbatonComponent> entity, ref StaminaDamageOnHitAttemptEvent args)
        {
            // WD EDIT START
            if (!_itemToggle.IsActivated(entity.Owner)
                || !TryGetBatteryComponent(entity, out var battery, out var batteryUid)
                || !_battery.TryUseCharge(batteryUid.Value, entity.Comp.EnergyPerUse, battery))
                args.Cancelled = true;
            // WD EDIT END
        }

        private void OnExamined(Entity<StunbatonComponent> entity, ref ExaminedEvent args)
        {
            var onMsg = _itemToggle.IsActivated(entity.Owner)
            ? Loc.GetString("comp-stunbaton-examined-on")
            : Loc.GetString("comp-stunbaton-examined-off");
            args.PushMarkup(onMsg);

            if (TryGetBatteryComponent(entity, out var battery, out _)) // WD EDIT
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
            if (TryGetBatteryComponent(entity, out var battery, out _)
                && battery.CurrentCharge >= entity.Comp.EnergyPerUse)
                return;

            args.Cancelled = true;

            if (args.User != null)
                _popup.PopupEntity(Loc.GetString("stunbaton-component-low-charge"), (EntityUid) args.User, (EntityUid) args.User);
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
            if (!TryGetBatteryComponent(entity, out var battery, out _)
                || battery.CurrentCharge < entity.Comp.EnergyPerUse)
                _itemToggle.TryDeactivate(entity.Owner, predicted: false);
        }

        private bool TryGetBatteryComponent(EntityUid uid, [NotNullWhen(true)] out BatteryComponent? battery,
            [NotNullWhen(true)] out EntityUid? batteryUid)
        {
            if (TryComp(uid, out battery))
            {
                batteryUid = uid;
                return true;
            }

            if (!_containers.TryGetContainer(uid, "cell_slot", out var container)
                || container is not ContainerSlot slot)
            {
                battery = null;
                batteryUid = null;
                return false;
            }

            batteryUid = slot.ContainedEntity;

            if (batteryUid != null)
                return TryComp(batteryUid, out battery);

            battery = null;
            return false;
        }
        // WD EDIT END
    }
}
