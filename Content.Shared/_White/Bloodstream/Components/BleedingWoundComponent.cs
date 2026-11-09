using Content.Shared._White.Bloodstream.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Bloodstream.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SharedBloodstreamSystem))]
public sealed partial class BleedingWoundComponent : Component
{
    /// <summary>
    /// Coefficient applied to damage to determine the rate at which bleeding occurs.
    /// </summary>
    [DataField]
    public FixedPoint2 BleedingCoefficient = 0.015f;

    /// <summary>
    /// The maximum amount of bleeding that can occur on a wound.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxBleeding = 20f;

    /// <summary>
    /// The threshold above which a wound requires tending to stop bleeding.
    /// </summary>
    [DataField]
    public FixedPoint2 RequiresTendingAbove = 25;

    /// <summary>
    /// The minimum amount of damage a wound must inflict to start causing bleeding.
    /// </summary>
    [DataField]
    public FixedPoint2 StartsBleedingAbove = 5;

    /// <summary>
    /// The coefficient used to determine the duration of bleeding based on damage.
    /// </summary>
    [DataField]
    public float BleedingDurationCoefficient = 1f;

    /// <summary>
    /// The current amount of bleeding.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 Bleeding;
}
