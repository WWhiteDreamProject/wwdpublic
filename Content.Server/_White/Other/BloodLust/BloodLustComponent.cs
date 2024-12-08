namespace Content.Server._White.Other.BloodLust;

[RegisterComponent]
public sealed partial class BloodLustComponent : Component
{
    [DataField]
    public float SprintModifier = 1.3f;

    [DataField]
    public float WalkModifier = 1.3f;

    [DataField]
    public float AttackRateModifier = 1.5f;
}
