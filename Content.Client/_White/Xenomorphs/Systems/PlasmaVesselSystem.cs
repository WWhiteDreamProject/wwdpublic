using Content.Client.Alerts;
using Content.Shared._White.Xenomorphs.Components;
using Content.Shared._White.Xenomorphs.Systems;

namespace Content.Client._White.Xenomorphs.Systems;

public sealed class PlasmaVesselSystem : SharedPlasmaVesselSystem
{
    public override void Initialize() =>
        SubscribeLocalEvent<PlasmaVesselComponent, UpdateAlertSpriteEvent>(OnUpdateAlertSprite);

    private void OnUpdateAlertSprite(EntityUid uid, PlasmaVesselComponent component, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != component.PlasmaAlert)
            return;

        var sprite = args.SpriteViewEnt.Comp;
        var plasma = Math.Clamp(component.Plasma.Int(), 0, 999);

        sprite.LayerSetState(PlasmaVisualLayers.Digit1, $"{plasma / 100 % 10}");
        sprite.LayerSetState(PlasmaVisualLayers.Digit2, $"{plasma / 10 % 10}");
        sprite.LayerSetState(PlasmaVisualLayers.Digit3, $"{plasma % 10}");
    }
}
