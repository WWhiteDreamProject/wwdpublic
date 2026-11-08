using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Overlays.Switchable;

[RegisterComponent, NetworkedComponent]
public sealed partial class ThermalVisionComponent : SwitchableOverlayComponent
{
    public override EntProtoId? ToggleAction { get; set; } = new("ToggleThermalVision");

    public override Color Color { get; set; } = Color.FromHex("#F84742");

    [DataField]
    public float LightRadius = 5f;
}

public sealed partial class ToggleThermalVisionEvent : InstantActionEvent;
