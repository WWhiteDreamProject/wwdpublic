namespace Content.Server._White.Event.CapturePoint;

[RegisterComponent]
public sealed partial class CapturePointCartridgeComponent : Component
{
    [DataField]
    public LocId TeamMessage;
}
