namespace Content.Server._White.AspectsSystem.Aspects.Components;

[RegisterComponent]
public sealed partial class TraitoredAspectComponent : Component
{
    [DataField]
    public int TimeElapsedForTraitors = 60;

    [DataField]
    public int TimeElapsedForAllMin = 300;

    [DataField]
    public int TimeElapsedForAllMax = 360;

    [DataField]
    public string AnnouncementForTraitorSound = "/Audio/_White/Aspects/palevo.ogg";
}
