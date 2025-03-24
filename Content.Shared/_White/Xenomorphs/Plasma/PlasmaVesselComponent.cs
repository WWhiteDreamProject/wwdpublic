using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Xenomorphs.Plasma;

/// <summary>
/// This is used for the plasma vessel component in the alien entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlasmaVesselComponent : Component
{
    /// <summary>
    /// The total amount of plasma the alien has.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Plasma = 0;

    /// <summary>
    /// The entity's current max amount of essence. Can be increased
    /// through harvesting player souls.
    /// </summary>
    [DataField]
    public FixedPoint2 PlasmaRegenCap = 500;

    /// <summary>
    /// The amount of plasma passively generated per second.
    /// </summary>
    [DataField]
    public FixedPoint2 PlasmaPerSecond = 0.2f;

    [DataField]
    public ProtoId<AlertPrototype> PlasmaAlert = "Plasma";

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PlasmaUnmodified = 0.2f;

    public float Accumulator = 0;

}

[NetSerializable, Serializable]
public enum PlasmaVisualLayers : byte
{
    Digit1,
    Digit2,
    Digit3,
}
