using Content.Shared.Damage;


// Damages the entity by a set amount when it hits someone.
// Can be used to make melee items limited-use or make an entity deal self-damage with unarmed attacks.
namespace Content.Server.Damage.Components;

[RegisterComponent]
public sealed partial class DamageOnHitComponent : Component
{
    [DataField("ignoreResistances")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IgnoreResistances = true;

    [DataField("damage", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = default!;
}
