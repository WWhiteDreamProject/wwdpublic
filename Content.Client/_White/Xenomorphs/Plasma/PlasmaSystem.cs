using Content.Client.Alerts;
using Content.Shared._White.Xenomorphs.Plasma;
using Content.Shared._White.Xenomorphs.Plasma.Components;
using Robust.Client.GameObjects;

namespace Content.Client._White.Xenomorphs.Plasma;

public sealed class PlasmaSystem : SharedPlasmaSystem
{
    [Dependency] private readonly SpriteSystem _system = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlasmaVesselComponent, UpdateAlertSpriteEvent>(OnUpdateAlertSprite);
    }

    private void OnUpdateAlertSprite(EntityUid uid, PlasmaVesselComponent component, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != component.PlasmaAlert)
            return;

        var sprite = args.SpriteViewEnt.AsNullable();
        var plasma = Math.Clamp(component.Plasma.Int(), 0, 999);

        _system.LayerSetRsiState(sprite, PlasmaVisualLayers.Digit1, $"{plasma / 100 % 10}");
        _system.LayerSetRsiState(sprite, PlasmaVisualLayers.Digit2, $"{plasma / 10 % 10}");
        _system.LayerSetRsiState(sprite, PlasmaVisualLayers.Digit3, $"{plasma % 10}");
    }
}
