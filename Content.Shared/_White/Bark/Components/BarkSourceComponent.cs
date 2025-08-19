namespace Content.Shared._White.Bark.Components;


[RegisterComponent]
public sealed partial class BarkSourceComponent : Component
{
    [DataField] public Queue<BarkData> Barks { get; set; } = new();
    [DataField] public IBarkAction Action;
    [ViewVariables] public BarkData? CurrentBark { get; set; }
    [ViewVariables] public float BarkTime { get; set; }
}
