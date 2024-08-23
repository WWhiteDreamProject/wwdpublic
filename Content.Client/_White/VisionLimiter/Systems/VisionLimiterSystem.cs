using Content.Client._White.VisionLimiter.Overlays;
using Content.Shared._White.VisionLimiter.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;

namespace Content.Client._White.VisionLimiter.Systems;

public sealed class VisionLimiterSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private VisionLimiterOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisionLimiterComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<VisionLimiterComponent, GotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<VisionLimiterComponent, ItemMaskToggledEvent>(OnMaskToggled);

        _overlay = new();
    }

    private void OnGotEquipped(EntityUid uid, VisionLimiterComponent comp, GotEquippedEvent args)
    {
        UpdateOverlay(comp, true);
    }

    private void OnGotUnequipped(EntityUid uid, VisionLimiterComponent comp, GotUnequippedEvent args)
    {
        UpdateOverlay(comp, false);
    }

    private void OnMaskToggled(EntityUid uid, VisionLimiterComponent comp, ItemMaskToggledEvent args)
    {
        UpdateOverlay(comp, !args.IsToggled);
    }

    private void UpdateOverlay(VisionLimiterComponent comp, bool active)
    {
        if (active)
        {
            _overlay.VisionLimitRadius = comp.Radius; // Assign the radius value from the component to the overlay
            _overlayMan.AddOverlay(_overlay);
        }
        else
            _overlayMan.RemoveOverlay(_overlay);
    }

}
