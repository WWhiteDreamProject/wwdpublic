using Content.Shared._White.Xenomorphs.Cast;
using Content.Shared.Damage;
using Content.Shared.Language;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Xenomorphs.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class XenomorphComponent : Component
{
    [DataField]
    public ProtoId<XenomorphCastePrototype> Caste = "Drone";

    /// <summary>
    /// Required damage specifier for healing provided by the weed.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier WeedHeal;

    /// <summary>
    /// Language on which xenomorph need to speak to send hivemind message.
    /// </summary>
    [DataField]
    public ProtoId<LanguagePrototype> XenoLanguageId { get; set; } = "XenoHivemind";

    [ViewVariables]
    public bool OnWeed;
}
