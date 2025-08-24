using Robust.Shared.Prototypes;


namespace Content.Shared._White.Bark.Components;


[RegisterComponent]
public sealed partial class ApplyBarkProtoComponent : Component
{
    [DataField(required:true)] public ProtoId<BarkVoicePrototype> VoiceProto { get; set; }
    [DataField] public BarkPercentageApplyData? PercentageApplyData { get; set; }
}
