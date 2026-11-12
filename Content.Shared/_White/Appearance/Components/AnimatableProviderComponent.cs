using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Appearance.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas:true)]
public sealed partial class AnimatableProviderComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionToggleWagging";

    [DataField]
    public string Suffix = "-animated";

    [ViewVariables, AutoNetworkedField]
    public bool Wagging;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? ActionEntity;
}
