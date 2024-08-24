using Content.Client._White.VisionLimit.Overlays;
using Content.Shared._White.VisionLimit.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._White.VisionLimit.Systems;

public sealed class VisionLimitSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private VisionLimitOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisionLimitComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<VisionLimitComponent, ComponentShutdown>(OnCompShutdown);

        SubscribeLocalEvent<VisionLimitComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<VisionLimitComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<ClothingLimitVisionComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ClothingLimitVisionComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<ClothingLimitVisionComponent, ItemMaskToggledEvent>(OnMaskToggled);

        _overlay = new();
        _overlay.ZIndex = -2; // Draw under damage overlay etc.
    }

    // Player entity interactions

    private void OnPlayerAttached(EntityUid uid, VisionLimitComponent component, LocalPlayerAttachedEvent args)
    {
        UpdateOverlay(component);
    }

    private void OnPlayerDetached(EntityUid uid, VisionLimitComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnCompInit(EntityUid uid, VisionLimitComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            UpdateOverlay(component);
    }

    private void OnCompShutdown(EntityUid uid, VisionLimitComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    // Equipment interactions

    private void OnGotEquipped(EntityUid uid, ClothingLimitVisionComponent comp, GotEquippedEvent args)
    {
        AddLimit(args.Equipee, comp);
    }

    private void OnGotUnequipped(EntityUid uid, ClothingLimitVisionComponent comp, GotUnequippedEvent args)
    {
        RemoveLimit(args.Equipee, comp);
    }

    private void OnMaskToggled(EntityUid uid, ClothingLimitVisionComponent comp, ItemMaskToggledEvent args)
    {
        if (!args.IsToggled && !args.IsEquip)
            AddLimit(args.Wearer, comp);
        else
            RemoveLimit(args.Wearer, comp);
    }

    // Overlay handling

    private void AddLimit(EntityUid uid, ClothingLimitVisionComponent limiter)
    {
        if (limiter.Radius == 0) // Radius 0 = no limit
            return;

        EnsureComp<VisionLimitComponent>(uid, out var comp);
        comp.VisionLimiters.Add(limiter, limiter.Radius);
        UpdateOverlay(comp);
    }

    private void RemoveLimit(EntityUid uid, ClothingLimitVisionComponent limiter)
    {
        if (TryComp<VisionLimitComponent>(uid, out var comp) && comp.VisionLimiters.ContainsKey(limiter))
        {
            comp.VisionLimiters.Remove(limiter);
            UpdateOverlay(comp);

            if (comp.VisionLimiters.Count == 0)
                RemComp<VisionLimitComponent>(uid);
        }

    }

    private void UpdateOverlay(VisionLimitComponent comp)
    {
        if (comp.VisionLimiters.Count > 0)
        {
            // Assign the smallest radius value from the VisionLimiters to the overlay
            var min = 1337f;

            foreach (var limiter in comp.VisionLimiters)
            {
                if (limiter.Value < min)
                    min = limiter.Value;
            }
            _overlay.VisionLimitRadius = min;

            _overlayMan.AddOverlay(_overlay);
        }
        else
            _overlayMan.RemoveOverlay(_overlay);
    }
}
