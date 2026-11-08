using Content.Shared.Inventory.Events;
using Content.Shared.Overlays.Switchable;
using Robust.Client.Graphics;
using Robust.Shared.Timing;

namespace Content.Client.Overlays.Switchable;

public sealed class ThermalVisionSystem : EquipmentHudSystem<ThermalVisionComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private ThermalVisionOverlay _thermalOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionComponent, SwitchableOverlayToggledEvent>(OnToggle);

        _thermalOverlay = new();
    }

    private void OnToggle(Entity<ThermalVisionComponent> ent, ref SwitchableOverlayToggledEvent args)
    {
        if (_gameTiming.IsFirstTimePredicted)
            RefreshOverlay();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ThermalVisionComponent> args)
    {
        base.UpdateInternal(args);
        ThermalVisionComponent? tvComp = null;
        var lightRadius = 0f;
        foreach (var comp in args.Components)
        {
            if (!comp.IsActive && (comp.PulseTime <= 0f || comp.PulseAccumulator >= comp.PulseTime))
                continue;

            if (tvComp == null)
                tvComp = comp;
            else if (!tvComp.DrawOverlay && comp.DrawOverlay)
                tvComp = comp;
            else if (tvComp.DrawOverlay == comp.DrawOverlay && tvComp.PulseTime > 0f && comp.PulseTime <= 0f)
                tvComp = comp;

            lightRadius = MathF.Max(lightRadius, comp.LightRadius);
        }

        UpdateThermalOverlay(tvComp, lightRadius);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _thermalOverlay.ResetLight(false);
        UpdateThermalOverlay(null, 0f);
    }

    private void UpdateThermalOverlay(ThermalVisionComponent? comp, float lightRadius)
    {
        _thermalOverlay.LightRadius = lightRadius;
        _thermalOverlay.Comp = comp;

        switch (comp)
        {
            case not null when !_overlayMan.HasOverlay<ThermalVisionOverlay>():
                _overlayMan.AddOverlay(_thermalOverlay);
                break;
            case null:
                _overlayMan.RemoveOverlay(_thermalOverlay);
                _thermalOverlay.ResetLight();
                break;
        }
    }

}
