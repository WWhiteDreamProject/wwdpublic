using Content.Shared._White.Damage;

namespace Content.Server._White.BloodCult.Runes.Revive;

[RegisterComponent]
public sealed partial class CultRuneReviveComponent : Component
{
    [DataField]
    public float ReviveRange = 0.5f;

    [DataField]
    public DamageSpecifier Healing = new()
    {
        ["Blunt"] = -33,
        ["Slash"] = -33,
        ["Piercing"] = -33,
        ["Heat"] = -33,
        ["Cold"] = -33,
        ["Shock"] = -33,
        ["Asphyxiation"] = -100,
        ["Bloodloss"] = -100,
        ["Poison"] = -50,
        ["Cellular"] = -50
    };
}
