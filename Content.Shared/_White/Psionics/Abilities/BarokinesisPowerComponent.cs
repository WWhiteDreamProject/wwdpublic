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
    public float MaximumWeakness = 1f; //full power

    [DataField]
    public float MinimumWeakness = 0.01f; //low power

    [DataField]
    public float MaximumRecovery = 10f; //in seconds

    [DataField]
    public float MinimumRecovery = 0.05f;

    [DataField]
    public TimeSpan LastUsedTime;
}
