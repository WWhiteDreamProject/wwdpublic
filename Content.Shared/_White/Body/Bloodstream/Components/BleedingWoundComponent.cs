using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Body.Bloodstream.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BleedingWoundComponent : Component
{
    /// <summary>
    /// Coefficient of damage to bleeding rate.
    /// </summary>
    [DataField]
    public FixedPoint2 BleedingCoefficient = 0.015f;

    /// <summary>
    /// The maximum amount of bleeding.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MaximumBleeding = 5f;

    /// <summary>
    /// Wound must be tended before bleeding ends if it has this much damage.
    /// </summary>
    [DataField]
    public FixedPoint2 RequiresTendingAbove = 25;

    /// <summary>
    /// Wound must have at least this much damage to start bleeding.
    /// </summary>
    [DataField]
    public FixedPoint2 StartsBleedingAbove = 10;

    /// <summary>
    /// Coefficient of damage to bleeding duration.
    /// </summary>
    [DataField]
    public float BleedingDurationCoefficient = 1f;

    /// <summary>
    /// The current amount of bleeding.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 Bleeding;
}
