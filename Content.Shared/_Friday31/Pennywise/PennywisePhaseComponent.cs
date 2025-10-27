using Robust.Shared.GameStates;

namespace Content.Shared._Friday31.Pennywise;

[RegisterComponent, NetworkedComponent]
public sealed partial class PennywisePhaseComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool IsPhasing = false;

    [DataField]
    public float Cooldown = 3f;

    public TimeSpan LastToggleTime = TimeSpan.Zero;
}
