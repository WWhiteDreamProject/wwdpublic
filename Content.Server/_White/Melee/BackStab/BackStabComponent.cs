namespace Content.Server._White.Melee.BackStab;

[RegisterComponent]
public sealed partial class BackStabComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DamageMultiplier = 2f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Angle Tolerance = Angle.FromDegrees(45d);
}
