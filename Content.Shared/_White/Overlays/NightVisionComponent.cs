using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Overlays;

[RegisterComponent, NetworkedComponent]
public sealed partial class NightVisionComponent : SwitchableOverlayComponent
{
    [DataField]
    public override string? ToggleAction { get; set; } = "ToggleNightVision";

    [DataField]
    public override SoundSpecifier? ActivateSound { get; set; }= new SoundPathSpecifier("/Audio/_White/Items/Goggles/activate.ogg");

    [DataField]
    public override SoundSpecifier? DeactivateSound { get; set; } = new SoundPathSpecifier("/Audio/_White/Items/Goggles/deactivate.ogg");

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public override Vector3 Tint { get; set; } = new(0.3f, 0.3f, 0.3f);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public override float Strength { get; set; } = 2f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public override float Noise { get; set; } = 0.5f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public override Color Color { get; set; } = Color.FromHex("#98FB98");
}

public sealed partial class ToggleNightVisionEvent : InstantActionEvent;
