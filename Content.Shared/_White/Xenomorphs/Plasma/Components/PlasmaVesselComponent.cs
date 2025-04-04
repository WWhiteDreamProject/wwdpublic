using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Xenomorphs.Plasma.Components;

/// <summary>
/// This is used for the plasma vessel component in the alien entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlasmaVesselComponent : Component
{
    /// <summary>
    /// The total amount of plasma the alien has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Plasma = 0;

    [DataField]
    public FixedPoint2 MaxPlasma = 300;

    /// <summary>
    /// The amount of plasma passively generated per second.
    /// </summary>
    [DataField]
    public FixedPoint2 PlasmaPerSecondOffWeed = 0.2f;

    /// <summary>
    /// The amount of plasma to which plasma per second will be equal, when alien stands on weeds.
    /// </summary>
    [DataField]
    public FixedPoint2 PlasmaPerSecondOnWeed = 15f;

    [DataField]
    public ProtoId<AlertPrototype> PlasmaAlert = "Plasma";

    [ViewVariables]
    public TimeSpan LastPointsAt;
}

[NetSerializable, Serializable]
public enum PlasmaVisualLayers : byte
{
    Digit1,
    Digit2,
    Digit3,
}
