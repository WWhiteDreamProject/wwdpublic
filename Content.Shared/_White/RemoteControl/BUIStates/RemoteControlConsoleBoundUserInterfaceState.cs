using System.Numerics;
using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._White.RemoteControl.BUIStates;

[Serializable, NetSerializable]
public sealed class RemoteControlConsoleBuiState : BoundUserInterfaceState
{
    // for the radar screen control
    public NavInterfaceState RadarState;

    public NetEntity? CurrentTurret;

    // should mirror LinkedTurrets in the turret component.
    public List<(NetEntity, bool)> LinkedTurrets;

    public RemoteControlConsoleError Error = RemoteControlConsoleError.None;

    public RemoteControlConsoleBuiState(NavInterfaceState state, NetEntity? currentTurret, List<(NetEntity, bool)> turrets)
    {
        RadarState = state;
        CurrentTurret = currentTurret;
        LinkedTurrets = turrets;
    }

    public RemoteControlConsoleBuiState(RemoteControlConsoleError error, NetEntity? currentTurret, List<(NetEntity, bool)> turrets)
    {
        RadarState = NavInterfaceState.Invalid;
        CurrentTurret = currentTurret;
        LinkedTurrets = turrets;
        Error = error;
    }
}


[Serializable, NetSerializable]
public sealed class RemoteControlConsoleUpdateAimDirectionMessage(Angle? newAimDir) : BoundUserInterfaceMessage
{
    public Angle? NewAimDirection = newAimDir;
}

[Serializable, NetSerializable]
public sealed class RemoteControlConsoleMouseClickMessage(NetCoordinates mousePos, bool down) : BoundUserInterfaceMessage
{
    public NetCoordinates MousePos = mousePos;
    public bool Down = down;
}

[Serializable, NetSerializable]
public sealed class RemoteControlConsoleTurretSelectedBuiMessage(NetEntity? turret) : BoundUserInterfaceMessage
{
    public NetEntity? Turret = turret;
}


public enum RemoteControlConsoleError
{
    None,
    NotConnected,
    NoPowerTurret,
    TurretDestroyed
}