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
    public EntityUid? CurrentTurret;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Shooting;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public Angle? CurrentAimDirection;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NavalTurretComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Name;

    [DataField, AutoNetworkedField]
    public Angle AngleTolerance = Math.PI / 720; // 0.25 degrees

    [DataField, AutoNetworkedField]
    public Angle RotationSpeed = Math.PI / 3;

    [DataField, AutoNetworkedField]
    public float AimpointTolerane = 0.5f;

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentConsole;
}

[Serializable, NetSerializable]
public enum NavalTurretConsoleUiKey : byte
{
    Key
}