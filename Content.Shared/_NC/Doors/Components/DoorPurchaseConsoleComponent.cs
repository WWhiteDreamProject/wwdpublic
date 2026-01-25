using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Doors.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DoorPurchaseConsoleComponent : Component
{
}

[Serializable, NetSerializable]
public enum DoorPurchaseConsoleUiKey
{
    Key
}
