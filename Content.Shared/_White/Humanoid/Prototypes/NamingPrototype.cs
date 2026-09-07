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
    [DataField(required:true)]
    public LocId Preset = "naming-preset-first";

    /// <summary>
    /// A dictionary mapping <see cref="Gender"/> to the dataset containing first names for that gender.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<Gender, ProtoId<LocalizedDatasetPrototype>> First = new();

    /// <summary>
    /// The dataset containing last names.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> Last;
}
