using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._White.Nutrition.Prototypes;

/// <summary>
/// This stores unique data for an item that is edible, such as verbs, verb icons, verb names, sounds, ect.
/// </summary>
[Prototype]
public sealed partial class IngestiblePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The localization identifier for the ingestion message.
    /// </summary>
    [DataField]
    public LocId Message;

    /// <summary>
    /// What type of food are we, currently used for determining verbs and some checks?
    /// </summary>
    [DataField]
    public LocId Name;

    /// <summary>
    /// Localization noun used when consuming this item.
    /// </summary>
    [DataField]
    public LocId Noun;

    /// <summary>
    /// Localization verb used when consuming this item.
    /// </summary>
    [DataField]
    public LocId Verb;

    /// <summary>
    /// The sound we make when eaten.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("eating");

    /// <summary>
    /// What type of food are we, currently used for determining verbs and some checks?
    /// </summary>
    [DataField]
    public SpriteSpecifier? Icon;
}
