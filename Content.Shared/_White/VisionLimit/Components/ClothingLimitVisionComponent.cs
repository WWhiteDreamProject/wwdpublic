namespace Content.Shared._White.VisionLimit.Components;

[RegisterComponent]
public sealed partial class ClothingLimitVisionComponent: Component
{
    /// <summary>
    /// Limits wearer's vision to roughly Radius tiles
    /// </summary>
    [DataField]
    public float Radius; // in tiles, 0 = no limit
}
