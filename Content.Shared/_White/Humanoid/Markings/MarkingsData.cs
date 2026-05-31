using Content.Shared._White.Humanoid.Markings.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Humanoid.Markings;

[DataDefinition]
[Serializable, NetSerializable]
public partial record struct MarkingsData
{
    /// <summary>
    /// The category of markings this data is associated with.
    /// </summary>
    [DataField]
    public ProtoId<MarkingCategoryPrototype> Category = new();

    /// <summary>
    /// The group of markings this data belongs to.
    /// </summary>
    [DataField]
    public ProtoId<MarkingGroupPrototype> Group = new();
}
