using Content.Shared._White.Damage;

namespace Content.Shared._White.Projectile;

[RegisterComponent]
public sealed partial class PenetratedProjectileComponent : Component
{
    [DataField]
    public float MinimumSpeed = 40f;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public EntityUid? PenetratedUid;
}
