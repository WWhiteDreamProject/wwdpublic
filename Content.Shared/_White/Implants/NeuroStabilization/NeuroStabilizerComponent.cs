namespace Content.Shared._White.Implants.NeuroStabilization;

[RegisterComponent]
public sealed partial class NeuroStabilizationComponent : Component
{
    [DataField]
    public bool Electrocution = true;

    [DataField]
    public TimeSpan TimeElectrocution = TimeSpan.FromSeconds(1);

    [DataField]
    public float DamageModifier = 0.66f;
}
