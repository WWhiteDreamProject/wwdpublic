using Content.Shared._White.Wounds.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Wounds.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(WoundableSystem))]
public sealed partial class WoundableProviderComponent : Component
{
    /// <summary>
    /// Threshold values for determining the severity of wounds to a given provider in relation to the damage received.
    /// </summary>
    [DataField]
    public SortedDictionary<FixedPoint2, WoundSeverity> Thresholds = new()
    {
        {0, WoundSeverity.Healthy},
        {50, WoundSeverity.Minor},
        {100, WoundSeverity.Moderate},
        {150, WoundSeverity.Severe},
        {200, WoundSeverity.Critical},
    };

    /// <summary>
    /// A container that contains wounds.
    /// </summary>
    [ViewVariables]
    public Container Container = new();

    /// <summary>
    /// The body entity containing this provider, if any.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Body;

    /// <summary>
    /// List of all provider wounds.
    /// </summary>
    [ViewVariables]
    public IReadOnlyList<EntityUid> Wounds => Container.ContainedEntities;

    /// <summary>
    /// The current severity of wounds to this provider.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public WoundSeverity Severity = WoundSeverity.Healthy;
}
