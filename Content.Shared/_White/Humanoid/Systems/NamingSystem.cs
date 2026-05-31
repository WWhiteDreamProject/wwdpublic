using Content.Shared._White.Humanoid.Prototypes;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._White.Humanoid.Systems;

public sealed class NamingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Generates a full name based on a specific <see cref="NamingPrototype"/> and gender.
    /// </summary>
    /// <param name="naming">The <see cref="ProtoId{NamingPrototype}"/> defining the naming convention to use.</param>
    /// <param name="gender">The gender to use for selecting the first name.</param>
    /// <returns>A generated full name string, or an empty string if any required prototype or dataset is missing.</returns>
    public string GenerateName(ProtoId<NamingPrototype> naming, Gender gender = Gender.Neuter)
    {
        if (!_prototype.TryIndex(naming, out var namingPrototype))
            return string.Empty;

        if (namingPrototype.First.TryGetValue(gender, out var firstList))
            return string.Empty;

        if (!_prototype.TryIndex(firstList, out var firstListPrototype))
            return string.Empty;

        if (!_prototype.TryIndex(namingPrototype.Last, out var lastListPrototype))
            return string.Empty;

        var first = Loc.GetString(_random.Pick(firstListPrototype.Values));
        var last = Loc.GetString(_random.Pick(lastListPrototype.Values));

        return Loc.GetString(namingPrototype.Preset, ("first", first), ("last", last));
    }

    /// <summary>
    /// Generates a full name based on a species and gender.
    /// </summary>
    /// <param name="species">The species to generate a name for.</param>
    /// <param name="gender">The gender to use for selecting the first name.</param>
    /// <returns>A generated full name string, or an empty string if any required prototype or dataset is missing.</returns>
    public string GenerateName(ProtoId<SpeciesPrototype> species, Gender gender = Gender.Neuter)
    {
        if (!_prototype.TryIndex(species, out var speciesPrototype))
            return string.Empty;

        return GenerateName(speciesPrototype.Naming, gender);
    }
}
