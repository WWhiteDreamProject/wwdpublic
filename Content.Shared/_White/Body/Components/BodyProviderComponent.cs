using System.Linq;
using Content.Shared._White.Body.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BodyProviderSlot
{
    public BodyProviderSlot() { }

    public BodyProviderSlot(BodyProviderSlot other)
    {
        Copy(other);
    }

    public BodyProviderSlot(BodyProviderType type, Dictionary<string, BodyProviderSlot> providers, EntProtoId? startingProvider)
    {
        Type = type;
        Providers = providers;
        StartingProvider = startingProvider;
    }

    [DataField]
    public BodyProviderType Type = BodyProviderType.None;

    [DataField]
    public Dictionary<string, BodyProviderSlot> Providers = new();

    [DataField(readOnly: true)]
    public EntProtoId? StartingProvider;

    [ViewVariables, NonSerialized]
    public ContainerSlot? ContainerSlot;

    public string? Id => ContainerSlot?.ID;
    public bool HasProvider => ContainerSlot?.ContainedEntity != null;
    public EntityUid? ProviderUid => ContainerSlot?.ContainedEntity;

    public void Copy(BodyProviderSlot other)
    {
        Type = other.Type;
        Providers = other.Providers.ToDictionary(x => x.Key, x => new BodyProviderSlot(x.Value));
        StartingProvider = other.StartingProvider;
    }
}
