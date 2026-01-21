using Robust.Shared.GameStates;

namespace Content.Shared._NC.Vehicle;

/// <summary>
///     Marker component for items that act as keys for a specific vehicle.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VehicleKeyComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Plate;
}
