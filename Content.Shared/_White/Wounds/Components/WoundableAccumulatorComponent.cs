using Content.Shared._White.Wounds.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Wounds.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(WoundableSystem))]
public sealed partial class WoundableAccumulatorComponent : Component
{
    /// <summary>
    /// Accumulator health.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Health = 100;

    /// <summary>
    /// Maximum accumulator health.
    /// </summary>
    [DataField]
    public FixedPoint2 MaximumHealth = 100;

    /// <summary>
    /// Threshold values for determining the severity of wounds to a given accumulator in relation to the damage received.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, WoundSeverity> Thresholds;

    /// <summary>
    /// The current severity of wounds to this accumulator.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public WoundSeverity Severity = WoundSeverity.Healthy;
}
