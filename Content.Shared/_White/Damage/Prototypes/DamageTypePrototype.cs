using Robust.Shared.Prototypes;

namespace Content.Shared._White.Damage.Prototypes;

/// <summary>
/// A single damage type.
/// </summary>
[Prototype]
public sealed partial class DamageTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name", required: true)]
    private LocId _name;

    /// <summary>
    /// Armor penetration level of this damage type.
    /// </summary>
    [DataField]
    public double ArmorPenetration;

    /// <summary>
    /// Wound formed when receiving this type of damage.
    /// </summary>
    [DataField]
    public EntProtoId? Wound;

    /// <summary>
    /// The group to which this type of damage belongs.
    /// </summary>
    [DataField]
    public ProtoId<DamageGroupPrototype> Group;

    [ViewVariables]
    public string Name => Loc.GetString(_name);
}
