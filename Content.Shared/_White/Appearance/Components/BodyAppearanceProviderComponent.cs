using Robust.Shared.GameStates;

namespace Content.Shared._White.Appearance.Components;

/// <summary>
/// Defines a body provider that applies a sprite to the specified <see cref="Layer"/> within the body.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class BodyAppearanceProviderComponent : Component
{
    /// <summary>
    /// Stores the specific data and appearance characteristics for this provider.
    /// </summary>
    [DataField, AutoNetworkedField, AlwaysPushInheritance]
    public BodyAppearanceData Appearance = new();

    /// <summary>
    /// List of additional provider entries that can be applied to body.
    /// </summary>
    [DataField, AutoNetworkedField, AlwaysPushInheritance]
    public List<BodyAppearanceProviderEntry> Entries = new();

    /// <summary>
    /// The specific layer that this provider is responsible for visualizing.
    /// </summary>
    [DataField(required: true)]
    public Enum Layer;

    /// <summary>
    /// The entities containing this provider.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> Entities = new();
}
