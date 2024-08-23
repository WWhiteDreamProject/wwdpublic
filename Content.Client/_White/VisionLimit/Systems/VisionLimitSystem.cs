using Content.Client._White.VisionLimit.Overlays;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;
using VisionLimitComponent = Content.Shared._White.VisionLimit.Components.VisionLimitComponent;

namespace Content.Client._White.VisionLimit.Systems;

public sealed class VisionLimitSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private VisionLimitOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisionLimitComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<VisionLimitComponent, GotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<VisionLimitComponent, ItemMaskToggledEvent>(OnMaskToggled);

        _overlay = new();
    }

    private void OnGotEquipped(EntityUid uid, VisionLimitComponent comp, GotEquippedEvent args)
    {
        UpdateOverlay(comp, true);
    }

    private void OnGotUnequipped(EntityUid uid, VisionLimitComponent comp, GotUnequippedEvent args)
    {
        UpdateOverlay(comp, false);
    }

    private void OnMaskToggled(EntityUid uid, VisionLimitComponent comp, ItemMaskToggledEvent args)
    {
        UpdateOverlay(comp, !args.IsToggled);
    }

    private void UpdateOverlay(VisionLimitComponent comp, bool active)
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
