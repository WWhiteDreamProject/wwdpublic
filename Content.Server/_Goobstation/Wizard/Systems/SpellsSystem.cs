using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._Goobstation.Wizard;
using Robust.Shared.Player;


namespace Content.Server._Goobstation.Wizard.Systems;


public sealed class SpellsSystem : SharedSpellsSystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    public override void Initialize()
    {

    }

    public override void CreateChargeEffect(EntityUid uid, ChargeSpellRaysEffectEvent ev)
    {
        RaiseNetworkEvent(ev, Filter.PvsExcept(uid));
    }

    protected override bool ChargeItem(EntityUid uid, ChargeMagicEvent ev)
    {
        if (!TryComp(uid, out BatteryComponent? battery) || battery.CurrentCharge >= battery.MaxCharge)
            return false;

        if (Tag.HasTag(uid, ev.WandTag))
        {
            var difference = battery.MaxCharge - battery.CurrentCharge;
            var charge = MathF.Min(difference, ev.WandChargeRate);
            var degrade = charge * ev.WandDegradePercentagePerCharge;
            var afterDegrade = MathF.Max(ev.MinWandDegradeCharge, battery.MaxCharge - degrade);
            if (battery.MaxCharge > ev.MinWandDegradeCharge)
                _battery.SetMaxCharge(uid, afterDegrade, battery);
            _battery.AddCharge(uid, charge, battery);
        }
        else
            _battery.SetCharge(uid, battery.MaxCharge, battery);

        PopupCharged(uid, ev.Performer, false);
        return true;
    }
}
