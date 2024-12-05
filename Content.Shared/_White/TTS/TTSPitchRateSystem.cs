using System.Diagnostics.CodeAnalysis;
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

    public bool TryGetPitchRate(EntityUid uid, [NotNullWhen(true)] out TTSPitchRate? pitch)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
        {
            pitch = new TTSPitchRate();
            return false;
        }

        pitch = GetPitchRate(humanoid.Species);
        return pitch != null;
    }

    public TTSPitchRate? GetPitchRate(ProtoId<SpeciesPrototype> protoId)
    {
        return SpeciesPitches.GetValueOrDefault(protoId);
    }
}

// ReSharper disable once InconsistentNaming
public record TTSPitchRate(string Pitch = "medium", string Rate = "medium");
