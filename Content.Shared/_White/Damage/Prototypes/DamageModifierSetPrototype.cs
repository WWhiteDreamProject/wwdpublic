using Robust.Shared.Prototypes;

namespace Content.Shared._White.Damage.Prototypes;

/// <summary>
/// A version of <see cref="DamageModifierSet"/>> that can be serialized as a prototype, but is functionally identical.
/// </summary>
[Prototype]
public sealed partial class DamageModifierSetPrototype : DamageModifierSet, IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
