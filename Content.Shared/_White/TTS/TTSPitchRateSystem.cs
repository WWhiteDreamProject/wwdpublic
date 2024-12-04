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

    public string TryGetPitchRate(EntityUid? uid, string text, string? speechRate = null, string? speechPitch = null)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return $"<speak>{text}</speak>";

        var species = SpeciesPitches.GetValueOrDefault(humanoid.Species);
        if (species == null)
            return $"<speak>{text}</speak>";

        if (speechRate != null)
            text = $"<prosody rate=\"{species.Rate}\">{text}</prosody>";
        if (speechPitch != null)
            text = $"<prosody pitch=\"{species.Pitch}\">{text}</prosody>";

        return $"<speak>{text}</speak>";
    }
}

// ReSharper disable once InconsistentNaming
public record TTSPitchRate(string Pitch = "medium", string Rate = "medium");
