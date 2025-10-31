using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Friday31.Slenderman;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlendermanShadowWalkComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionSlendermanShadowWalk";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public bool InShadow;
}
