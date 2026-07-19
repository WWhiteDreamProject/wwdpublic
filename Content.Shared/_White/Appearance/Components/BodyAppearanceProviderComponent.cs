using Content.Shared._White.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Appearance.Components;

/// <summary>
/// Defines a body provider that applies a sprite to the specified <see cref="Layer"/> within the body.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class BodyAppearanceProviderComponent : Component
{
    /// <summary>
    /// Stores the specific data and appearance characteristics for this provider's layer.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public BodyAppearanceData Appearance = new();

    /// <summary>
    /// The specific layer that this provider is responsible for visualizing.
    /// </summary>
    [DataField(required: true)]
    public Enum Layer;

    /// <summary>
    /// The specific body coloration. of this provider.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<BodyColorationPrototype> Coloration;

    /// <summary>
    /// Holds specific data associated with the prototype layer.
    /// </summary>
    [DataField(required: true)]
    public PrototypeLayerData Data;

    /// <summary>
    /// The body entity containing this provider, if any.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Body;
}
