using Content.Shared._White.Wounds.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Wounds.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(WoundableSystem))]
public sealed partial class WoundableResistComponent : Component
{
    /// <summary>
    /// The resistance value depends on the severity of the wounds.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<WoundSeverity, FixedPoint2> Thresholds;

    /// <summary>
    /// The current wound resistance.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 CurrentResistance;
}
