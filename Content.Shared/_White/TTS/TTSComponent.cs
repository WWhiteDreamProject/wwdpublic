using Robust.Shared.Prototypes;

namespace Content.Shared._White.TTS;

[RegisterComponent, AutoGenerateComponentState]
// ReSharper disable once InconsistentNaming
public sealed partial class TTSComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<TTSVoicePrototype> Prototype { get; set; } = "Eugene";
}
