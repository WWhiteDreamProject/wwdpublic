using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Appearance.Components;

/// <summary>
/// Defines a body provider that applies a sprite to the specified <see cref="Layer"/> within the body.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BodyAppearanceProviderComponent : Component
{
    /// <summary>
    /// Stores the specific data and appearance characteristics for this provider.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public BodyAppearanceData Appearance = new();

    /// <summary>
    /// List of additional provider entries that can be applied to body.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public List<BodyAppearanceProviderEntry> Entries = new();

    /// <summary>
    /// The specific layer that this provider is responsible for visualizing.
    /// </summary>
    [DataField(required: true)]
    public Enum Layer;

    /// <summary>
    /// The entities containing this provider.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> Entities = new();
}

[Serializable, NetSerializable]
public sealed class BodyAppearanceProviderComponentState(BodyAppearanceProviderComponent component, EntityManager entityManager) : ComponentState
{
    public readonly BodyAppearanceData Appearance = component.Appearance;
    public readonly List<BodyAppearanceProviderEntry> Entries = new(component.Entries);
    public readonly HashSet<NetEntity> Entities = entityManager.GetNetEntitySet(component.Entities);
}
