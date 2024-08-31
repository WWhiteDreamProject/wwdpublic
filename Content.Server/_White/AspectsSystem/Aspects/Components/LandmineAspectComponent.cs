namespace Content.Server._White.AspectsSystem.Aspects.Components;

[RegisterComponent]
public sealed partial class LandmineAspectComponent : Component
{
    [DataField]
    public int Min = 40;

    [DataField]
    public int Max = 60;
}
