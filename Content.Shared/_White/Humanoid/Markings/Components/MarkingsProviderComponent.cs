using Content.Shared._White.Appearance.Components;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared.DisplacementMap;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;


namespace Content.Shared._White.Humanoid.Markings.Components;

/// <summary>
/// Defines a body provider that applies markings on top of the layer specified in <see cref="BodyAppearanceProviderComponent" />
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class MarkingsProviderComponent : Component
{
    /// <summary>
    /// Optional displacement data for this provider to apply to markings.
    /// Only applies to markings which support displacement data.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Enum, DisplacementData> Displacement = new();

    /// <summary>
    /// A mapping that defines which layers depend on others for hiding.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Enum, HashSet<Enum>> DependentHidingLayers = new();

    /// <summary>
    /// The dictionary of layer to markings this provider is currently providing to the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Enum, List<Marking>> Markings = new();

    /// <summary>
    /// A collection of layers that are eligible to be hidden by this provider's logic.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<Enum> HideableLayers = new();

    /// <summary>
    /// Stores the specific data for this provider's.
    /// </summary>
    [DataField(required: true), AlwaysPushInheritance]
    public MarkingData Data;

    /// <summary>
    /// The body entity containing this provider, if any.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Body;

    /// <summary>
    /// Stores the last markings that were successfully applied.
    /// </summary>
    /// <remarks>Client only</remarks>
    [ViewVariables]
    public List<Marking> Applied = new();
}
