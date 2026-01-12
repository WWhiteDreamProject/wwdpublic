using Robust.Shared.GameStates;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Shared._White.Psionics.Abilities;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class TelekinesisPowerComponent : Component
{
    [DataField]
    public EntityUid? TetheredEntity;


    [DataField, AutoNetworkedField]
    public EntityCoordinates TargetCoordinates;

    [ViewVariables, AutoNetworkedField]
    public Vector2 TargetPosition;

    [ViewVariables]
    public TimeSpan LastUpdate;

    [DataField]
    public EntityUid? TetherPoint;

    [DataField]
    public string JointId = "telekinesis";

    [DataField]
    public float FollowSpeed = 15f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float MaxMass = 100f;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float MaxForce = 40f;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float Frequency = 2.5f;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float DampingRatio = 0.8f;
}
