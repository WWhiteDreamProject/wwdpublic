using Robust.Shared.Serialization;

namespace Content.Shared._White.Mobs;

/// <summary>
/// Defines visual parameters for mob state overlay effects, controlling vignette appearance and animation.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public partial record struct MobStateOverlayData
{
    /// <summary>
    /// The base color of the overlay effect.
    /// </summary>
    [DataField]
    public Color Color = Color.Transparent;

    /// <summary>
    /// The alpha value for the inner overlay layer.
    /// </summary>
    [DataField]
    public float InnerAlpha;

    /// <summary>
    /// The maximum screen area occupied by the inner overlay layer.
    /// </summary>
    [DataField]
    public float InnerMax;

    /// <summary>
    /// The minimum screen area occupied by the inner overlay layer.
    /// </summary>
    [DataField]
    public float InnerMin;

    /// <summary>
    /// The coefficient of increase of the inner overlay layer during pulsation.
    /// </summary>
    [DataField]
    public float InnerPulse;

    /// <summary>
    /// The alpha value for the outer overlay layer.
    /// </summary>
    [DataField]
    public float OuterAlpha;

    /// <summary>
    /// The maximum screen area occupied by the outer overlay layer.
    /// </summary>
    [DataField]
    public float OuterMax;

    /// <summary>
    /// The minimum screen area occupied by the outer overlay layer.
    /// </summary>
    [DataField]
    public float OuterMin;

    /// <summary>
    /// The coefficient of increase of the outer overlay layer during pulsation.
    /// </summary>
    [DataField]
    public float OuterPulse;

    /// <summary>
    /// The rate at which the overlay animates or pulses over time.
    /// </summary>
    [DataField]
    public float Rate;
}
