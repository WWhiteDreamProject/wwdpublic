using Content.Shared._White.Body.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Body.Components;

/// <summary>
/// Marks an entity as being able to be inserted into an entity with <see cref="BodyComponent" />.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBodySystem))]
public sealed partial class BodyProviderComponent : Component
{
    /// <summary>
    /// The type of this body provider.
    /// </summary>
    [DataField("providerType")] // It can't support the "type" tag. Sad 🥲
    public BodyProviderType Type = BodyProviderType.None;

    /// <summary>
    /// Body providers attached to this entity.
    /// </summary>
    [DataField]
    public Dictionary<string, BodyProviderSlot> Providers = new();

    /// <summary>
    /// The body entity containing this provider, if any.
    /// </summary>
    [ViewVariables]
    public EntityUid? Body;

    /// <summary>
    /// The parent entity containing this provider, if any.
    /// </summary>
    [ViewVariables]
    public EntityUid? Parent;

    /// <summary>
    /// The ancestors entities containing this provider, if any.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> Ancestors = new();
}

[Serializable, NetSerializable]
public sealed class BodyProviderComponentState(BodyProviderComponent component, EntityManager entityManager) : ComponentState
{
    public readonly Dictionary<string, BodyProviderSlot> Providers = new(component.Providers);
    public readonly NetEntity? Body = entityManager.GetNetEntity(component.Body);
    public readonly NetEntity? Parent = entityManager.GetNetEntity(component.Parent);
    public readonly HashSet<NetEntity> Ancestors = entityManager.GetNetEntitySet(component.Ancestors);
}
