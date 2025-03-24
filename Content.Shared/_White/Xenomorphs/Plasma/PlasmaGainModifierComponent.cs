using Content.Shared.FixedPoint;

namespace Content.Shared._White.Xenomorphs.Plasma;

[RegisterComponent]
public sealed partial class PlasmaGainModifierComponent : Component
{
    /// <summary>
    /// The amount of plasma to which plasma per second will be equal, when alien stands on weeds.
    /// </summary>
    [DataField]
    public FixedPoint2 PlasmaPerSecond = 15f;
}
