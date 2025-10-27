using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Friday31.Slenderman;

[RegisterComponent, NetworkedComponent]
public sealed partial class SlendermanDismemberAbilityComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionSlendermanDismember";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public float ScreamerDuration = 5f;
}
