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

    [DataField("name", required: true)]
    private LocId _name;

    [ViewVariables]
    public string Name => Loc.GetString(_name);
}
