using Content.Shared._White.Appearance.Prototypes;
using Content.Shared._White.Humanoid.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Appearance;

[DataDefinition]
[Serializable, NetSerializable]
public partial record struct BodyAppearanceData
{
    /// <summary>
    /// The color groups associated with this appearance data.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<BodyColorGroupPrototype>, Color> ColorGroups = new();

    /// <summary>
    /// The body type associated with this appearance data.
    /// </summary>
    [DataField]
    public ProtoId<BodyTypePrototype> BodyType = "Normal";

    /// <summary>
    /// The sex associated with this appearance data.
    /// </summary>
    [DataField]
    public Sex Sex = Sex.Unsexed;
}
