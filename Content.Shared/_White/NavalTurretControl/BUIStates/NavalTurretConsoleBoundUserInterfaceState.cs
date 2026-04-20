using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.NavalTurretControl.BUIStates;

[Serializable, NetSerializable]
public sealed class NavalTurretConsoleBuiState : BoundUserInterfaceState
{
    // for the radar screen control
    public NavInterfaceState State;

    public NavalTurretConsoleError Error = NavalTurretConsoleError.None;

    public NavalTurretConsoleBuiState(NavInterfaceState state)
    {
        State = state;
    }

    public NavalTurretConsoleBuiState(NavalTurretConsoleError error)
    {
        State = NavInterfaceState.Invalid;
        Error = error;
    }

}

public enum NavalTurretConsoleError
{
    None,
    NotConnected,
    NoPowerTurret,
    TurretDestroyed
}