using Robust.Shared.Prototypes;

namespace Content.Shared._White.Damage.Prototypes;

/// <summary>
/// A group of <see cref="DamageTypePrototype"/>s.
/// </summary>
[Prototype]
public sealed partial class DamageGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Name of this damage group.
    /// </summary>
    [DataField(required: true)]
    private LocId Name { get; set; }

    /// <summary>
    /// Damage types supported by this group.
    /// </summary>
    [DataField]
    public List<ProtoId<DamageTypePrototype>> Types = new();

    /// <summary>
    /// Localized name of this damage group.
    /// </summary>
    [ViewVariables]
    public string LocalizedName => Loc.GetString(Name);
}
