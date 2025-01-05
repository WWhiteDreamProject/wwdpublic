using Content.Shared.Aliens.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aliens.Components;

/// <summary>
/// The AcidMakerComponent is used for managing the acid production process.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AcidMakerSystem)), AutoGenerateComponentState]
public sealed partial class AcidMakerComponent : Component
{
    /// <summary>
    /// The text that pops up whenever making acid fails for not having enough plasma.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public string PopupText = "alien-action-fail-plasma";

    /// <summary>
    /// What will be produced at the end of the action.
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public EntProtoId EntityProduced;

    /// <summary>
    /// The entity needed to actually make acid. This will be granted (and removed) upon the entity's creation.
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public EntProtoId Action;

    /// <summary>
    /// Optional reference to the entity performing the acid-making action.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// This will subtract (not add, don't get this mixed up) from the current plasma of the mob making acid.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float PlasmaCost = 300f;

}
