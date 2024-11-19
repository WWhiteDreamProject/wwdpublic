using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

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
    [ViewVariables(VVAccess.ReadWrite), DataField("maxPlasma")]
    public FixedPoint2 PlasmaRegenCap = 500;

    [ViewVariables]
    public FixedPoint2 PlasmaPerSecond = 0.2f;

    /// <summary>
    /// The amount of plasma passively generated per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("plasmaPerSecond")]
    public FixedPoint2 PlasmaUnmodified = 0.2f;

    [ViewVariables]
    public float Accumulator = 0;

    /// <summary>
    /// The amount of plasma to which plasma per second will be equal, when alien stands on weeds.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("plasmaModified")]
    public float WeedModifier = 0.2f;

    /// <summary>
    /// Alert value for tracking alert state.
    /// </summary>
    [ViewVariables]
    public int AlertValue { get; set; } = -1;

    /// <summary>
    /// Last time the alert was updated.
    /// </summary>
    [ViewVariables]
    public float LastAlertUpdateTime { get; set; } = 0;

    /// <summary>
    /// Interval for alert updates in seconds.
    /// </summary>
    public const float AlertUpdateInterval = 1.0f;

}
