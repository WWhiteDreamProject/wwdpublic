namespace Content.Shared._White.Psionics.Abilities;

[RegisterComponent]
public sealed partial class BarokinesisPowerComponent : Component
{
    [DataField]
    public float ThrowSpeed = 10f;

    [DataField]
    public float BaseForce = 5f;

    [DataField]
    public bool CheckPsionics = true;
}
