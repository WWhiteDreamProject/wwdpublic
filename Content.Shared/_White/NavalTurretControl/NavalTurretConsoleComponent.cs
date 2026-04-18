using Robust.Shared.Serialization;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System.Numerics;
using Robust.Shared.Map;

namespace Content.Shared._White.NavalTurretControl;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NavalTurretConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedTurret;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NavalTurretComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedConsole;
}

[NetSerializable, Serializable]
public sealed partial class NavalTurretAimpointBoundUserInterfaceMessage(NetCoordinates aimpoint) : BoundUserInterfaceMessage
{
    public NetCoordinates Aimpoint = aimpoint;
}


[Serializable, NetSerializable]
public sealed class RequestNavalTurretShootEvent : EntityEventArgs
{
    public NetEntity Console;
    public NetCoordinates Coordinates;
}

[Serializable, NetSerializable]
public sealed class RequestNavalTurretStopShootEvent : EntityEventArgs
{
    public NetEntity Console;
}


[Serializable, NetSerializable]
public sealed class RequestNavalTurretRotationEvent(Angle angle) : EntityEventArgs
{
    public Angle Angle = angle;
    public NetEntity Console;
}


[Serializable, NetSerializable]
public enum NavalTurretConsoleUiKey : byte
{
    Key
}