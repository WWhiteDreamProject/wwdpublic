using Content.Shared._White.Damage;

namespace Content.Shared.Wieldable.Components;

[RegisterComponent, Access(typeof(SharedWieldableSystem))]
public sealed partial class IncreaseDamageOnWieldComponent : Component
{
    [DataField("damage", required: true)]
    public DamageSpecifier BonusDamage = default!;
}
