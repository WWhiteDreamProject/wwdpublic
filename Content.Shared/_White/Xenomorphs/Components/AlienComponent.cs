using Content.Shared.Damage;
using Content.Shared.Language;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Xenomorphs.Components;

/// <summary>
/// The AlienComponent is used to manage the abilities and properties of alien entities.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AlienComponent : Component
{
    /// <summary>
    /// The caste type of the alien.
    /// </summary>
    [DataField]
    public string GreetingText;

    /// <summary>
    /// Required damage specifier for healing provided by the weed.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier WeedHeal;

    /// <summary>
    /// Language on which alien need to speak to send hivemind message.
    /// </summary>
    [DataField]
    public ProtoId<LanguagePrototype> XenoLanguageId { get; set; } = "XenoHivemind";

    [ViewVariables]
    public bool OnWeed;
}
