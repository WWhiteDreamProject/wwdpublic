using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Overlays.Switchable;

[RegisterComponent, NetworkedComponent]
public sealed partial class NightVisionComponent : SwitchableOverlayComponent
{
    public override EntProtoId? ToggleAction { get; set; } = new EntProtoId("ToggleNightVision");

    [ViewVariables(VVAccess.ReadWrite)]
    public override Color Color { get; set; } = Color.FromHex("#98FB98");
}

public sealed partial class ToggleNightVisionEvent : InstantActionEvent;
