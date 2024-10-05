namespace Content.Client._White.Guns.Stretched;

[RegisterComponent, Access(typeof(StretchedVisualizerSystem))]
public sealed partial class StretchedVisualsComponent : Component
{
    [DataField(required: true)]
    public string LoadedState = string.Empty;

    [DataField(required: true)]
    public string StretchedState = string.Empty;

    [DataField(required: true)]
    public string UnstrungState = string.Empty;
}
