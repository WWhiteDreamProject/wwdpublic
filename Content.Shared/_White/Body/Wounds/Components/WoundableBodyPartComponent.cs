using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Body.Wounds.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WoundableBodyPartComponent : Component
{
    /// <summary>
    /// Threshold values for determining the severity of wounds to a given body part in relation to the damage received.
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
    /// Parent body for this part.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Parent;

    /// <summary>
    /// List of all body part wounds.
    /// </summary>
    [ViewVariables]
    public IReadOnlyList<EntityUid> Wounds => Container.ContainedEntities;

    /// <summary>
    /// The current severity of wounds to this body part.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public WoundSeverity WoundSeverity;
}
