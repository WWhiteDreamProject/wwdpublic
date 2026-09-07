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

    /// <summary>
    /// Name of this damage type.
    /// </summary>
    [DataField(required: true)]
    private LocId Name { get; set; }

    /// <summary>
    /// Wound formed when receiving this type of damage.
    /// </summary>
    [DataField]
    public EntProtoId? Wound;

    /// <summary>
    /// Localized name of this damage type.
    /// </summary>
    [ViewVariables]
    public string LocalizedName => Loc.GetString(Name);
}
