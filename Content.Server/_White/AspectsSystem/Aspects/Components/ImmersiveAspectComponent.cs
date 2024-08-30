namespace Content.Server._White.AspectsSystem.Aspects.Components;

[RegisterComponent]
public sealed partial class ImmersiveAspectComponent : Component
{
    [DataField]
    public float EyeModifier = 0.6f;

    [DataField]
    public float TelescopeDivisor = 0.15f;

    [DataField]
    public float TelescopeLerpAmount = 0.07f;
}
