using Content.Shared._White.Body.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Body.Components;

/// <summary>
/// Marks an entity as being able to be inserted into an entity with <see cref="BodyComponent" />.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SharedBodySystem))]
public sealed partial class BodyProviderComponent : Component
{
    /// <summary>
    /// Body providers attached to this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, BodyProviderSlot> Providers = new();

    /// <summary>
    /// The body entity containing this provider, if any.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Body;

    /// <summary>
    /// The parent entity containing this provider, if any.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Parent;

    /// <summary>
    /// The type of this body provider.
    /// </summary>
    [DataField("providerType")] // It can't support the "type" tag. Sad 🥲
    public BodyProviderType Type = BodyProviderType.None;
}
