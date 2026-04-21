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

    [DataField, AutoNetworkedField]
    public Angle AngleTolerance = Math.PI / 360; // 0.5 degrees

    [DataField, AutoNetworkedField]
    public Angle RotationSpeed = Math.PI / 3;

    [DataField, AutoNetworkedField]
    public float AimpointTolerane = 0.5f;

    /// <summary>
    /// Relative to self.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public Vector2? CurrentAimpoint;
}

[Serializable, NetSerializable]
public enum NavalTurretConsoleUiKey : byte
{
    Key
}