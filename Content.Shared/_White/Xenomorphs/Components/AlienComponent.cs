using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Language;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Xenomorphs.Components;

/// <summary>
/// The AlienComponent is used to manage the abilities and properties of alien entities.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class AlienComponent : Component
{
    /// <summary>
    /// This will subtract (not add, don't get this mixed up) from the current plasma of the mob making node.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PlasmaCostNode = 50f;

    /// <summary>
    /// The prototype ID for the weed node to use.
    /// </summary>
    [DataField]
    public EntProtoId WeednodePrototype = "ResinWeedNode";

    /// <summary>
    /// The prototype ID for the action associated with the weed node.
    /// </summary>
    [DataField]
    public EntProtoId? WeednodeAction = "ActionSpawnResinNode";

    /// <summary>
    /// The caste type of the alien.
    /// </summary>
    [DataField]
    public string GreetingText;

    /// <summary>
    /// Optional reference to the entity associated with the weed node action.
    /// </summary>
    [DataField]
    public EntityUid? WeednodeActionEntity;

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
}

/// <summary>
/// Event for the instant action related to weed node actions.
/// </summary>
public sealed partial class WeednodeActionEvent : InstantActionEvent { }
