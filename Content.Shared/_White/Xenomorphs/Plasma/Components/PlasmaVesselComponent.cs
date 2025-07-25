using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Xenomorphs.Plasma.Components;

/// <summary>
/// This is used for the plasma vessel component in the xenomorph entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlasmaVesselComponent : Component
{
    /// <summary>
    /// The total amount of plasma the xenomorph has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Plasma = 200;

    [DataField]
    public FixedPoint2 MaxPlasma = 500;

    /// <summary>
    /// The amount of plasma passively generated per second.
    /// </summary>
    [DataField]
    public FixedPoint2 PlasmaPerSecondOffWeed = 0.75f;

    /// <summary>
    /// The amount of plasma to which plasma per second will be equal, when xenomorph stands on weeds.
    /// </summary>
    [DataField]
    public FixedPoint2 PlasmaPerSecondOnWeed = 7.5f;

    [DataField]
    public ProtoId<AlertPrototype> PlasmaAlert = "Plasma";

    [ViewVariables]
    public TimeSpan NextPointsAt;
}

[NetSerializable, Serializable]
public enum PlasmaVisualLayers : byte
{
    Digit1,
    Digit2,
    Digit3,
}
