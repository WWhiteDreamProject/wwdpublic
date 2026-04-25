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
    [AutoNetworkedField]
    public EntityUid? CurrentTurret;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Shooting;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public Angle? CurrentAimDirection;

    // not used on client
    public List<EntityUid> LinkedTurrets = new();
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NavalTurretComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Name;

    [DataField, AutoNetworkedField]
    public Angle AngleTolerance = Math.PI / 360; // 0.5 degrees

    [DataField, AutoNetworkedField]
    public Angle RotationSpeed = Math.PI / 3;

    [DataField, AutoNetworkedField]
    public float AimpointTolerane = 0.5f;

    public List<EntityUid> LinkedConsoles = new();
    public EntityUid? CurrentConsole;

}

[Serializable, NetSerializable]
public enum NavalTurretConsoleUiKey : byte
{
    Key
}