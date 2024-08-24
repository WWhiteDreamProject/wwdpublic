namespace Content.Shared._White.VisionLimit.Components;

[RegisterComponent]
public sealed partial class VisionLimitComponent: Component
{
    /// <summary>
    /// Stores values of limits applied to an entity by any VisionLimiterComponent
    /// </summary>

    [DataField]
    public List<float> Limiters = [];

}
