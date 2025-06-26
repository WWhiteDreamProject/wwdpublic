using Robust.Shared.GameStates;

namespace Content.Shared._White.Overlays;

/// <summary>
/// This is used for IPCs with studio visor that reduces CRT vision effects.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StudioVisorOverlayComponent : Component;
