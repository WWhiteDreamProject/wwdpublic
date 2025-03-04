namespace Content.Server._Goobstation.Ghostbar.Components;

/// <summary>
/// Tracker for ghostbar players
/// </summary>
[RegisterComponent]
public sealed partial class GhostBarPlayerComponent : Component
{
    // wwdp start
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public TimeSpan TimeOfDeath = TimeSpan.Zero;
    // wwdp end
}
