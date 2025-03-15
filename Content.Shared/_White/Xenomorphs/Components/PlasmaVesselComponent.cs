using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

/// <summary>
/// This is used for the plasma vessel component in the alien entities.
/// </summary>
[RegisterComponent, NetworkedComponent]
public partial class PlasmaVesselComponent : Component
{
    /// <summary>
    /// The total amount of plasma the alien has.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Plasma = 0;

    /// <summary>
    /// The entity's current max amount of essence. Can be increased
    /// through harvesting player souls.
    /// </summary>
    [DataField]
    public FixedPoint2 PlasmaRegenCap = 500;

    public FixedPoint2 PlasmaPerSecond = 0.2f;

    /// <summary>
    /// The amount of plasma passively generated per second.
    /// </summary>
    [DataField]
    public FixedPoint2 PlasmaUnmodified = 0.2f;

    public float Accumulator = 0;

    /// <summary>
    /// The amount of plasma to which plasma per second will be equal, when alien stands on weeds.
    /// </summary>
    [DataField]
    public float WeedModifier = 15;

    /// <summary>
    /// Alert value for tracking alert state.
    /// </summary>
    public int AlertValue { get; set; } = -1;

    /// <summary>
    /// Last time the alert was updated.
    /// </summary>
    public float LastAlertUpdateTime { get; set; } = 0;

    /// <summary>
    /// Interval for alert updates in seconds.
    /// </summary>
    public const float AlertUpdateInterval = 1.0f;

}
