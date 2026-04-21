using System.Numerics;
using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Map;
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


[Serializable, NetSerializable]
public sealed class RequestNavalTurretShootBuiMessage(Vector2 aimpoint, NetEntity console) : BoundUserInterfaceMessage
{
    public Vector2 RelativeAimpoint = aimpoint;
    public NetEntity Console = console;
}

[Serializable, NetSerializable]
public sealed class RequestNavalTurretStopShootBuiMessage(NetEntity console) : BoundUserInterfaceMessage
{
    public NetEntity Console = console;
}


[Serializable, NetSerializable]
public sealed class RequestNavalTurretUpdateAimpointBuiMessage(Vector2 aimpoint, NetEntity console) : BoundUserInterfaceMessage
{
    public Vector2 RelativeAimpoint = aimpoint;
    public NetEntity Console = console;
}

public enum NavalTurretConsoleError
{
    None,
    NotConnected,
    NoPowerTurret,
    TurretDestroyed
}