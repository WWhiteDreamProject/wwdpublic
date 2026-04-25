using System.Numerics;
using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._White.NavalTurretControl.BUIStates;

[Serializable, NetSerializable]
public sealed class NavalTurretConsoleBuiState : BoundUserInterfaceState
{
    // for the radar screen control
    public NavInterfaceState RadarState;
    
    // this bullshit is due to me builidng around the radar system.
    // When it is refactored, this should be changed to something sane aswell.
    public NetEntity? CurrentTurret => RadarState.Coordinates?.NetEntity;

    // should mirror LinkedTurrets in the turret component.
    public List<NetEntity> LinkedTurrets;

    public NavalTurretConsoleError Error = NavalTurretConsoleError.None;

    public NavalTurretConsoleBuiState(NavInterfaceState state, List<NetEntity> turrets)
    {
        RadarState = state;
        LinkedTurrets = turrets;
    }

    public NavalTurretConsoleBuiState(NavalTurretConsoleError error, List<NetEntity> turrets)
    {
        RadarState = NavInterfaceState.Invalid;
        LinkedTurrets = turrets;
        Error = error;
    }
}


[Serializable, NetSerializable]
public sealed class NavalTurretConsoleUpdateAimDirectionMessage(Angle? newAimDir) : BoundUserInterfaceMessage
{
    public Angle? NewAimDirection = newAimDir;
}

[Serializable, NetSerializable]
public sealed class NavalTurretConsoleMouseClickMessage(NetCoordinates mousePos, bool down) : BoundUserInterfaceMessage
{
    public NetCoordinates MousePos = mousePos;
    public bool Down = down;
}

[Serializable, NetSerializable]
public sealed class NavalTurretConsoleTurretSelectedBuiMessage(NetEntity? turret) : BoundUserInterfaceMessage
{
    public NetEntity? Turret = turret;
}


public enum NavalTurretConsoleError
{
    None,
    NotConnected,
    NoPowerTurret,
    TurretDestroyed
}