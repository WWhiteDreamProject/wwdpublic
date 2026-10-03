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
    
    public RemoteControlVisualMode VisualMode;

    // should mirror LinkedTurrets in the turret component.
    public List<(NetEntity, bool)> LinkedTurrets;

    public RemoteControlConsoleError Error = RemoteControlConsoleError.None;

    public RemoteControlConsoleBuiState(NavInterfaceState state, NetEntity? currentTurret, RemoteControlVisualMode visMode, List<(NetEntity, bool)> turrets)
    {
        RadarState = state;
        CurrentTurret = currentTurret;
        VisualMode = visMode;
        LinkedTurrets = turrets;
    }

    public RemoteControlConsoleBuiState(RemoteControlConsoleError error, NetEntity? currentTurret, RemoteControlVisualMode visMode, List<(NetEntity, bool)> turrets)
    {
        RadarState = NavInterfaceState.Invalid;
        CurrentTurret = currentTurret;
        VisualMode = visMode;
        LinkedTurrets = turrets;
        Error = error;
    }
}


[Serializable, NetSerializable]
public sealed class RemoteControlConsoleUpdateAimDirectionMessage(Angle? newAimDir, NetEntity? aimtarget) : BoundUserInterfaceMessage
{
    public Angle? NewAimDirection = newAimDir;
    public NetEntity? AimTarget = aimtarget;
    
}

[Serializable, NetSerializable]
public sealed class RemoteControlConsoleMouseClickMessage(bool down) : BoundUserInterfaceMessage
{
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