namespace Content.Server._White.Guns;

[RegisterComponent]
public sealed partial class PoweredComponent : Component
{
    [DataField]
    public float EnergyPerUse = 180f;

    [DataField]
    public float ProjectileSpeedModified = 15f;
}
