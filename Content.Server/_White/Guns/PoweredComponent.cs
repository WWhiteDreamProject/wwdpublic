namespace Content.Server._White.Guns;

[RegisterComponent]
public sealed partial class PoweredComponent : Component
{
    [DataField]
    public float EnergyPerUse = 180f;

    /// <summary>
    /// Modifies the speed of projectiles fired from this powered weapon.
    /// </summary>
    [DataField]
    public float ProjectileSpeedModified = 15f;
}
