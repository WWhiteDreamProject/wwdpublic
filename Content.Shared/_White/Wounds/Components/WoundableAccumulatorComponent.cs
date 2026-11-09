using Content.Shared._White.Body;
using Content.Shared._White.Wounds.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Wounds.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(WoundableSystem))]
public sealed partial class WoundableAccumulatorComponent : Component
{
    /// <summary>
    /// Determines which children body provider should inherit the damage change.
    /// </summary>
    [DataField]
    public BodyProviderType Heir = ~BodyProviderType.Part;

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
    [DataField]
    public SortedDictionary<FixedPoint2, WoundSeverity> Thresholds = new()
    {
        {90, WoundSeverity.Healthy},
        {70, WoundSeverity.Minor},
        {50, WoundSeverity.Moderate},
        {25, WoundSeverity.Severe},
        {0, WoundSeverity.Critical},
    };

    /// <summary>
    /// The current severity of wounds to this accumulator.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public WoundSeverity Severity = WoundSeverity.Healthy;
}
