using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Medical.Pain.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PainfulWoundComponent : Component
{
    /// <summary>
    /// How much pain does a wound cause, regardless of the amount of damage.
    /// </summary>
    [DataField]
    public FixedPoint2 Pain = FixedPoint2.Zero;

    /// <summary>
    /// Coefficients for damage to pain.
    /// </summary>
    [DataField(required: true)]
    public float PainCoefficients;

    /// <summary>
    /// Coefficients for damage to initial pain.
    /// </summary>
    [DataField(required: true)]
    public float FreshPainCoefficients;

    [DataField]
    public double FreshPainDecreasePerSecond = 0.15d;
}
