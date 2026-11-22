using Content.Shared._White.Skeleton.Systems;
using Content.Shared._White.Wounds;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Skeleton.Components;

/// <summary>
/// SkeletonProvider/Bone protects its inner providers.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SkeletonSystem))]
public sealed partial class SkeletonProviderComponent : Component
{
    /// <summary>
    /// Strength of an intact bone: the flat amount of mechanical damage it absorbs per hit.
    /// </summary>
    [DataField]
    public FixedPoint2 Strength;

    /// <summary>
    /// The resistance value depends on the severity of the wounds.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<WoundSeverity, FixedPoint2> Thresholds;

    /// <summary>
    /// The parent entity containing this provider, if any.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Parent;

    /// <summary>
    /// The current severity of wounds to this provider.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public WoundSeverity Severity = WoundSeverity.Healthy;
}
