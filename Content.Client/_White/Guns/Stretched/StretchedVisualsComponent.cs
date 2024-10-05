namespace Content.Client._White.Guns.Stretched;

[RegisterComponent, Access(typeof(StretchedVisualizerSystem))]
public sealed partial class StretchedVisualsComponent : Component
{
    [DataField]
    public string? LoadedState;

    [DataField]
    public string? StretchedState;

    [DataField]
    public string? UnstrungState;
}
