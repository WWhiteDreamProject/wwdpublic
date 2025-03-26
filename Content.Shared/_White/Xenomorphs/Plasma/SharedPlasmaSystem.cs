using Content.Shared._White.Xenomorphs.Plasma.Components;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Placeable;

namespace Content.Shared._White.Xenomorphs.Plasma;

public abstract class SharedPlasmaSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        // PlasmaTransfer
        SubscribeLocalEvent<PlasmaTransferComponent, ComponentStartup>(OnPlasmaTransferStartup);
        SubscribeLocalEvent<PlasmaTransferComponent, ComponentShutdown>(OnPlasmaTransferShutdown);
        SubscribeLocalEvent<PlasmaTransferComponent, TransferPlasmaActionEvent>(OnPlasmaTransfer);

        // PlasmaGainModifier
        SubscribeLocalEvent<PlasmaGainModifierComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<PlasmaGainModifierComponent, ItemRemovedEvent>(OnItemRemoved);
    }

    private void OnPlasmaTransferStartup(EntityUid uid, PlasmaTransferComponent comp, ComponentStartup args) =>
        _actions.AddAction(uid, ref comp.ActionEntity, comp.Action);

    private void OnPlasmaTransferShutdown(EntityUid uid, PlasmaTransferComponent comp, ComponentShutdown args) =>
        _actions.RemoveAction(uid, comp.ActionEntity);

    private void OnPlasmaTransfer(EntityUid uid, PlasmaTransferComponent component, TransferPlasmaActionEvent args)
    {
        if (args.Handled
            || !TryComp(args.Target, out PlasmaVesselComponent? plasmaVesselTarget)
            || !ChangePlasmaAmount(uid, -component.Amount))
            return;

        ChangePlasmaAmount(args.Target, component.Amount, plasmaVesselTarget);

        args.Handled = true;
    }

    private void OnItemPlaced(EntityUid uid, PlasmaGainModifierComponent component, ItemPlacedEvent args)
    {
        if (!TryComp<PlasmaVesselComponent>(args.OtherEntity, out var plasmaVessel)
            || plasmaVessel.PlasmaPerSecond == component.PlasmaPerSecond)
            return;

        plasmaVessel.PlasmaUnmodified = plasmaVessel.PlasmaPerSecond;
        plasmaVessel.PlasmaPerSecond = component.PlasmaPerSecond;
    }

    private void OnItemRemoved(EntityUid uid, PlasmaGainModifierComponent component, ItemRemovedEvent args)
    {
        if (!TryComp<PlasmaVesselComponent>(args.OtherEntity, out var plasmaVessel))
            return;

        plasmaVessel.PlasmaPerSecond = plasmaVessel.PlasmaUnmodified;
    }

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
