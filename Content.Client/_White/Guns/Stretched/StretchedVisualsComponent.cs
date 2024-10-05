namespace Content.Client._White.Guns.Stretched;

[RegisterComponent, Access(typeof(StretchedVisualizerSystem))]
public sealed partial class StretchedVisualsComponent : Component
{
    [DataField(required: true)]
    public string? LoadedState;

    [DataField(required: true)]
    public string? StretchedState;

    [DataField(required: true)]
    public string? UnstrungState;
}
