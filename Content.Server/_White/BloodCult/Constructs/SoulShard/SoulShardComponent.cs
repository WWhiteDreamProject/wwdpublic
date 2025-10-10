using Robust.Shared.Prototypes;

namespace Content.Server._White.BloodCult.Constructs.SoulShard;

[RegisterComponent]
public sealed partial class SoulShardComponent : Component
{
    [DataField]
    public bool IsBlessed;

    [DataField]
    public Color BlessedLightColor = Color.LightCyan;

    [DataField]
    public EntProtoId ShadeProto = "MobBloodCultShade";

    [DataField]
    public EntProtoId PurifiedShadeProto = "MobHolyShade";

    public EntityUid? ShadeUid;
}
