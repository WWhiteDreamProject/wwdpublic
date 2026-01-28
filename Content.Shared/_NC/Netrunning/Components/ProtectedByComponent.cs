using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;

namespace Content.Shared._NC.Netrunning.Components;

/// <summary>
/// Marks a device as protected by a NetServer.
/// Added/removed by NetServerSystem when device connects/disconnects from power network.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ProtectedByComponent : Component
{
    /// <summary>
    /// The NetServer entity that protects this device.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Server;
}
