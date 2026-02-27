using Content.Shared._White.Humanoid.Markings;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Body.Components;

/// <summary>
/// Defines an organ/body part/bone that applies a sprite to the specified <see cref="Layer" /> within the body
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class BodyAppearanceProviderComponent : Component
{
    /// <summary>
    /// The data for the layer.
    /// </summary>
    [DataField, AutoNetworkedField]
    public BodyAppearanceLayerData Data = new();

    /// <summary>
    /// A dictionary of layers to other layers that visually depend on them for hiding, e.g. SnoutCover depends on Snout.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Enum, HashSet<Enum>> DependentHidingLayers = new();

    /// <summary>
    /// The list of markings this provider is currently providing to the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Enum, List<Marking>> Markings = new();

    /// <summary>
    /// The layer on the entity that this contributes to.
    /// </summary>
    [DataField(required: true)]
    public Enum Layer;

    /// <summary>
    /// Layers that are eligible for hiding based on e.g. clothing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<Enum> HideableLayers = new();

    /// <summary>
    /// Defines the type of markings this provider can take.
    /// </summary>
    [DataField(required: true), AlwaysPushInheritance]
    public MarkingProviderData MarkingData;

    /// <summary>
    /// Client only - the last markings applied by this component.
    /// </summary>
    [ViewVariables]
    public List<Marking> AppliedMarkings = new();
}

/// <summary>
/// Defines the layers and group an provider takes markings for.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public partial record struct MarkingProviderData
{
    [DataField(required: true)]
    public HashSet<Enum> Layers = default!;

    /// <summary>
    /// The type of provider this is for markings.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MarkingsGroupPrototype> Group = default!;
}
