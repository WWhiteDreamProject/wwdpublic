namespace Content.Shared._White.VisionLimiter.Components;

[RegisterComponent]
public sealed partial class VisionLimiterComponent: Component
{
    [DataField]
    public float Radius; // in tiles, 0 = no limit
}
