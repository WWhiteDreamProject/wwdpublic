namespace Content.Server._White.AspectsSystem.Aspects.Components;

[RegisterComponent]
public sealed partial class PresentAspectComponent : Component
{
    [DataField]
    public int Min = 150;

    [DataField]
    public int Max = 200;
}
