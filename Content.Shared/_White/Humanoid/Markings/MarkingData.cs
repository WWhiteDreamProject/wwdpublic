using Content.Shared._White.Humanoid.Markings.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Humanoid.Markings;

[DataDefinition]
[Serializable, NetSerializable]
public partial record struct MarkingData
{
    /// <summary>
    /// The specific layers that this data is responsible for visualizing.
    /// </summary>
    [DataField]
    public HashSet<Enum> Layers = new();

    /// <summary>
    /// The group of markings this data belongs to.
    /// </summary>
    [DataField]
    public ProtoId<MarkingGroupPrototype> Group = new();
}
