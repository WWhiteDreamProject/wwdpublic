using Content.Shared._White.Humanoid.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Bark.Components;

[RegisterComponent]
public sealed partial class ApplyBarkProtoComponent : Component
{
    public static ProtoId<BarkVoicePrototype> DefaultVoice = HumanoidProfileSystem.DefaultBark;

    [DataField]
    public ProtoId<BarkVoicePrototype> VoiceProto { get; set; } = DefaultVoice;

    [DataField]
    public BarkPercentageApplyData? PercentageApplyData { get; set; }
}
