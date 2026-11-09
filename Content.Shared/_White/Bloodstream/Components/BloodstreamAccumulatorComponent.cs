using Content.Shared._White.Bloodstream.Systems;
using Content.Shared._White.Wounds;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Bloodstream.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SharedBloodstreamSystem))]
public sealed partial class BloodstreamAccumulatorComponent : Component
{
    [DataField]
    public Dictionary<WoundSeverity, FixedPoint2> ReductionThresholds = new()
    {
        {WoundSeverity.Healthy, 0.11f},
        {WoundSeverity.Minor, 0.075f},
        {WoundSeverity.Moderate, 0.04f},
        {WoundSeverity.Severe, 0.015f},
        {WoundSeverity.Critical, 0f},
    };

    /// <summary>
    /// The amount of bleeding reduction applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Reduction = 0.11f;
}
