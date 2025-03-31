namespace Content.Server._White.Hearing;

[RegisterComponent]
public sealed partial class HearingComponent: Component
{
    // Stores a list of deafness sources
    // Used by the DeafnessSystem to apply DeafComponent to this entity
    [DataField]
    public List<DeafnessSource> DeafnessSources = [];
}
