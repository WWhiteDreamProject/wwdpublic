using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Friday31.Pennywise;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PennywiseNoclipComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionPennywiseNoclip";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public bool IsNoclip;
}
