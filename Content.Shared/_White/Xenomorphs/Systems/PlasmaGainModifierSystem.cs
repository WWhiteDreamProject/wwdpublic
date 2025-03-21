using Content.Shared._White.Xenomorphs.Components;
using Content.Shared.Placeable;

namespace Content.Shared._White.Xenomorphs.Systems;

public sealed class PlasmaGainModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PlasmaGainModifierComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<PlasmaGainModifierComponent, ItemRemovedEvent>(OnItemRemoved);
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
}
