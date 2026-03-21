namespace Content.Shared._White.Psionics.Abilities;

[RegisterComponent]
public sealed partial class BarokinesisPowerComponent : Component
{
    [DataField]
    public float ThrowSpeed = 10f;

    [DataField]
    public float BaseForce = 5f; //if not psiopic

    [DataField]
    public bool CheckPsionics = true;

    [DataField]
    public float CurrentWeakness = 1f; //1 weakness its all power, 0.1 weakness its small power

    [DataField]
    public float MaximumWeakness = 1f;

    [DataField]
    public float MinimumWeakness = 0.01f;

    [DataField]
    public float DecayWeakness = 0.3f; //decay per usage

    [DataField]
    public float RecoveryWeakness = 0.03f; //recovery per sometimes
}
