using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Medical.Wounds.Components.Wound;

[RegisterComponent, NetworkedComponent, EntityCategory("Wounds")]
public sealed partial class BleedingWoundComponent : Component
{
    /// <summary>
    /// Coefficient of damage to bleeding rate.
    /// </summary>
    [DataField(required: true)]
    public float BleedingCoefficients;

    /// <summary>
    /// Coefficient of damage to bleeding duration.
    /// </summary>
    [DataField(required: true)]
    public float BleedingDurationCoefficients;

    /// <summary>
    /// Wound must have at least this much damage to start bleeding.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 StartsBleedingAbove;

    /// <summary>
    /// Wound must be tended before bleeding ends if it has this much damage.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 RequiresTendingAbove;
}
