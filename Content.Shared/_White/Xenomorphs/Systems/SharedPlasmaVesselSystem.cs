using Content.Shared._White.Xenomorphs.Components;
using Content.Shared.Alert;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Xenomorphs.Systems;

public abstract class SharedPlasmaVesselSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public bool ChangePlasmaAmount(EntityUid uid, FixedPoint2 amount, PlasmaVesselComponent? component = null, bool regenCap = false)
    {
        if (!Resolve(uid, ref component) || component.Plasma + amount < 0)
            return false;

        component.Plasma += amount;

        if (regenCap)
            component.Plasma = FixedPoint2.Min(component.Plasma, component.PlasmaRegenCap);

        Dirty(uid, component);

        _alerts.ShowAlert(uid, component.PlasmaAlert);

        return true;
    }
}
