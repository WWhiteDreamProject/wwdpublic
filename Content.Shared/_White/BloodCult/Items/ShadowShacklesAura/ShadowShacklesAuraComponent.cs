using Content.Shared._White.BloodCult.Items.BaseAura;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.BloodCult.Items.ShadowShacklesAura;

[RegisterComponent]
public sealed partial class ShadowShacklesAuraComponent : BaseAuraComponent
{
    [DataField]
    public EntProtoId ShacklesProto = "ShadowShackles";

    [DataField]
    public TimeSpan MuteDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public float DistanceThreshold = 1.5f;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid Shackles;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid Target;
}
