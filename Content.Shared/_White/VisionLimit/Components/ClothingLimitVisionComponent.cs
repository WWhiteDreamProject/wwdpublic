namespace Content.Shared._White.VisionLimit.Components;

[RegisterComponent]
public sealed partial class ClothingLimitVisionComponent: Component
{
    [DataField]
    public float Radius; // in tiles, 0 = no limit
}
