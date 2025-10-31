using Robust.Shared.Prototypes;

namespace Content.Shared._Friday31.Jason;

[RegisterComponent]
public sealed partial class JasonDecapitateAbilityComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionJasonDecapitate";

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ActionEntity;
}
