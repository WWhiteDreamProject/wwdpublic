using Content.Shared.Inventory.Events;
using Content.Shared.Overlays.Switchable;
using Robust.Client.Graphics;
using Robust.Shared.Timing;

namespace Content.Client.Overlays.Switchable;

public sealed class NightVisionSystem : EquipmentHudSystem<NightVisionComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private NightVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, SwitchableOverlayToggledEvent>(OnToggle);

        _overlay = new()
        {
            IsActive = false
        };
        _overlayMan.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnToggle(Entity<NightVisionComponent> ent, ref SwitchableOverlayToggledEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        _overlay.SetParams(ent.Comp.Tint, ent.Comp.Strength, ent.Comp.Noise, ent.Comp.Color, ent.Comp.PulseTime);
        RefreshOverlay();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<NightVisionComponent> args)
    {
        var active = false;
        NightVisionComponent? nvComp = null;
        foreach (var comp in args.Components)
        {
            if (comp.IsActive || comp.PulseTime > 0f && comp.PulseAccumulator < comp.PulseTime)
                active = true;
            else
                continue;

            if (comp.DrawOverlay)
            {
                if (nvComp == null || nvComp.PulseTime > 0f && comp.PulseTime <= 0f)
                    nvComp = comp;
            }

            if (active && nvComp is { PulseTime: <= 0 })
                break;
        }

        UpdateNightVision(active);
    }

    protected override void DeactivateInternal() => UpdateNightVision(false);

    private void UpdateNightVision(bool active)
    {
        _lightManager.DrawLighting = !active;
        _overlay.IsActive = active;
    }
}
