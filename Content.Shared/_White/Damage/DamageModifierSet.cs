using Content.Shared._White.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Damage;

/// <summary>
/// A set of coefficients or flat modifiers to damage types.
/// Can be applied to <see cref="DamageSpecifier"/>.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
[Virtual]
public partial class DamageModifierSet
{
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, float> Coefficients = new();

    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> FlatReduction = new();
}
