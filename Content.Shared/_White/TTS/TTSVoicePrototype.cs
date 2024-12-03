using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.TTS;

[Prototype("ttsVoice")]
// ReSharper disable once InconsistentNaming
public sealed class TTSVoicePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField]
    public string Name { get; } = string.Empty;

    [DataField(required: true)]
    public Sex Sex { get; }

    [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
    public string Speaker { get; } = string.Empty;

    /// <summary>
    /// Whether the voice is available in the character editor.
    /// </summary>
    [DataField]
    public bool RoundStart { get; } = true;

    [DataField]
    public bool SponsorOnly { get; }

    [DataField]
    public bool BorgVoice { get; }
}
