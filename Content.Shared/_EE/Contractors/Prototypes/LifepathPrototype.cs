using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Customization.Systems;
using Content.Shared.Traits;


namespace Content.Shared._EE.Contractors.Prototypes;

/// <summary>
/// Prototype representing a character's Lifepath in YAML.
/// </summary>
[Prototype("lifepath")]
public sealed partial class LifepathPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public string NameKey { get; private set; } = string.Empty;

    [DataField]
    public string DescriptionKey { get; private set; } = string.Empty;

    [DataField]
    public List<CharacterRequirement> Requirements = new();

    [DataField(serverOnly: true)]
    public TraitFunction[] Functions { get; private set; } = Array.Empty<TraitFunction>();
}
