using Content.Shared._White.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.BloodCult.Runes;

[Prototype]
public sealed class BloodCultRunePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField]
    public float DrawTime = 4f;

    [DataField]
    public bool RequireTargetDead;

    [DataField]
    public int RequiredTotalCultists = 1;

    /// <summary>
    ///     Damage dealt on the rune drawing.
    /// </summary>
    [DataField]
    public DamageSpecifier DrawDamage = new() {  ["Slash"] = 15, };
}
