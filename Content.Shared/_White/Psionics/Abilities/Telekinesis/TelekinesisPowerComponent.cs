using Robust.Shared.GameStates;

namespace Content.Shared._White.Psionics.Abilities;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class TelekinesisPowerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? TetheredEntity;

    [ViewVariables]
    public TimeSpan LastUpdate;

    [DataField]
    public EntityUid? TetherPoint;

    [DataField]
    public string? JointId;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float MaxMass = 30f;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float MaxForce = 60f;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float Frequency = 1.7f;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float DampingRatio = 0.7f;
}
