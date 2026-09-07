using Content.Shared._White.Appearance.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

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
    /// The specific color group of this provider.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<BodyColorGroupPrototype> Group;

    /// <summary>
    /// The specific color of this provider.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color = Color.White;

    /// <summary>
    /// The specific sprite path of this provider.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string Path;

    /// <summary>
    /// The specific sprite state of this provider.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string State;

    /// <summary>
    /// The body entity containing this provider, if any.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Body;
}
