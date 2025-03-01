namespace Content.Server._White.AspectsSystem.Aspects.Components;

[RegisterComponent]
public sealed partial class RenameCrewAspectComponent : Component
{
    [DataField]
    public List<string> FirstNames = new List<string>();
    [DataField]
    public List<string> LastNames = new List<string>();
    [DataField]
    public float SpecialNameChance = 0.05f;
    [DataField]
    public List<string> SpecialNames = new List<string>();

    public List<(int, int)> RegularNameCombos = new();
}

