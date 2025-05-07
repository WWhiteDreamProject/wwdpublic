using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

// ReSharper disable once InconsistentNaming

namespace Content.Shared._White.TTS;

/// <summary>
/// Apply TTS for entity chat say messages
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TTSComponent : Component
{
    /// <summary>
    /// Prototype of used voice for TTS.
    /// </summary>
    [DataField("voice")]
    public ProtoId<TTSVoicePrototype> VoicePrototypeId = "Eugene";
}
