using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.TTS;

// ReSharper disable once InconsistentNaming
public sealed class TTSPitchRateSystem : EntitySystem
{
    public readonly Dictionary<ProtoId<SpeciesPrototype>, TTSPitchRate> SpeciesPitches = new()
    {
        ["SlimePerson"] = new TTSPitchRate("high"),
        ["Arachnid"] = new TTSPitchRate("x-high", "x-fast"),
        ["Dwarf"] = new TTSPitchRate("high", "slow"),
        ["Human"] = new TTSPitchRate(),
        ["Diona"] = new TTSPitchRate("x-low", "x-slow"),
        ["Reptilian"] = new TTSPitchRate("low", "slow"),
    };

    public string GetFormattedSpeechText(EntityUid? uid, string text, string? speechRate = null, string? speechPitch = null)
    {
        var ssml = text;
        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
        {
            var species = SpeciesPitches.GetValueOrDefault(humanoid.Species);
            if (species != null)
            {
                speechRate ??= species.Rate;
                speechPitch ??= species.Pitch;
            }
        }

        if (speechRate != null)
            ssml = $"<prosody rate=\"{speechRate}\">{ssml}</prosody>";
        if (speechPitch != null)
            ssml = $"<prosody pitch=\"{speechPitch}\">{ssml}</prosody>";

        return $"<speak>{ssml}</speak>";
    }
}

// ReSharper disable once InconsistentNaming
public record TTSPitchRate(string Pitch = "medium", string Rate = "medium");
