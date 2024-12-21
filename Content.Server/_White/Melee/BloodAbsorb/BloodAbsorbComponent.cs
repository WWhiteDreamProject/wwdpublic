namespace Content.Server._White.Melee.BloodAbsorb;

[RegisterComponent]
public sealed partial class BloodAbsorbComponent : Component
{
    [DataField]
    public bool BloodLust;

    [DataField]
    public int MinAbsorb = 1;

    [DataField]
    public int MaxAbsorb = 20;

    [DataField]
    public float AbsorbModifierOnHeavy = 0.7f;
}
