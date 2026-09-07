using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Body;

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
