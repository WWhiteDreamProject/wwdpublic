using System;
using Content.Shared.Alert;
using Content.Shared.Aliens.Components;
using Content.Shared.Chat;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Aliens.Systems;

/// <summary>
/// This handles the plasma vessel component.
/// </summary>
public sealed class SharedPlasmaVesselSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [ValidatePrototypeId<AlertPrototype>]
    public ProtoId<AlertPrototype> PlasmaCounterAlert = "PlasmaCounter";

    public bool ChangePlasmaGain(EntityUid uid, float modifier, PlasmaVesselComponent? component = null)
    {
        if (component == null)
        {
            return false;
        }
        component.PlasmaPerSecond *= modifier;
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<PlasmaVesselComponent>();

        while (query.MoveNext(out var uid, out var alien))
        {
            alien.Accumulator += frameTime;

            // Делаем проверку только раз в секунду
            if (alien.Accumulator < 1)
                continue;

            alien.Accumulator -= 1;

            bool weed = false;
            foreach (var entity in _lookup.GetEntitiesInRange(Transform(uid).Coordinates, 0.1f))
            {
                if (HasComp<PlasmaGainModifierComponent>(entity))
                {
                    alien.PlasmaPerSecond = alien.WeedModifier;
                    weed = true;
                }
            }

            if (!weed)
                alien.PlasmaPerSecond = alien.PlasmaUnmodified;

            if (alien.Plasma < alien.PlasmaRegenCap)
            {
                ChangePlasmaAmount(uid, alien.PlasmaPerSecond, alien, regenCap: true);
            }
        }
    }

    public bool ChangePlasmaAmount(EntityUid uid, FixedPoint2 amount, PlasmaVesselComponent? component = null, bool regenCap = false)
    {
        if (!Resolve(uid, ref component) || component == null)
        {
            return false;
        }

        component.Plasma += amount;

        if (regenCap)
        {
            component.Plasma = FixedPoint2.Min(component.Plasma, component.PlasmaRegenCap);
        }

        var stalk = CompOrNull<AlienStalkComponent>(uid);
        if (stalk != null && stalk.IsActive)
        {
            return true;
        }

        if (amount != component.PlasmaUnmodified && amount != component.WeedModifier)
        {
            _popup.PopupEntity(Loc.GetString("alien-plasma-left", ("value", component.Plasma)), uid, uid);
        }

        int newAlertValue = (int)(component.Plasma.Float() / 50);

        if (newAlertValue != component.AlertValue)
        {
            if (_gameTiming != null)
            {
                float currentTime = (float)_gameTiming.CurTime.TotalSeconds;

                if (currentTime - component.LastAlertUpdateTime >= PlasmaVesselComponent.AlertUpdateInterval)
                {
                    _alerts.ShowAlert(uid, PlasmaCounterAlert, (short)newAlertValue);
                    component.AlertValue = newAlertValue;
                    component.LastAlertUpdateTime = currentTime;
                }
            }
        }

        return true;
    }
}
