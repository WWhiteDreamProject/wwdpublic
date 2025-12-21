using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._White.BloodCult.Items.BloodSpear;

[RegisterComponent]
public sealed partial class BloodSpearComponent : Component
{
    [DataField]
    public EntityUid? Master;

    [DataField]
    public TimeSpan ParalyzeTime = TimeSpan.FromSeconds(4);

    [DataField]
    public EntProtoId RecallActionId = "ActionBloodCultBloodSpearRecall";

    public EntityUid? RecallAction;

    [DataField]
    public SoundSpecifier RecallAudio = new SoundPathSpecifier(
        new ResPath("/Audio/_White/Magic/BloodCult/rites.ogg"),
        AudioParams.Default.WithVolume(-3));
}
