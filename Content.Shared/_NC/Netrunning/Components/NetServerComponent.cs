using Robust.Shared.GameStates;

namespace Content.Shared._NC.Netrunning.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NetServerComponent : Component
{
    // Marker for Net Servers
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Password = "admin"; // Default password
}
