using Robust.Shared.GameStates;

namespace Content.Shared._Friday31.AdminNotifyOnPickup;

[RegisterComponent, NetworkedComponent]
public sealed partial class AdminNotifyOnPickupComponent : Component
{
    [DataField(required: true)]
    public string Message = string.Empty;
}
