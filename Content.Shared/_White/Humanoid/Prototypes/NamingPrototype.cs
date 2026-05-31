using Content.Shared.Dataset;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Prototypes;

[Prototype]
public sealed partial class NamingPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// A string specifying a general format string for combining the first and last names.
    /// </summary>
    [DataField]
    public LocId Preset = "naming-preset-first";

    /// <summary>
    /// A dictionary mapping <see cref="Gender"/> to the dataset containing first names for that gender.
    /// </summary>
    [DataField]
    public Dictionary<Gender, ProtoId<LocalizedDatasetPrototype>> First = new()
    {
        { Gender.Neuter, "NamingFirst" },
        { Gender.Epicene, "NamingFirst" },
        { Gender.Female, "NamingFirstFemale" },
        { Gender.Male, "NamingFirstMale" },
    };

    /// <summary>
    /// The dataset containing last names.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> Last = "NamingLast";
}
