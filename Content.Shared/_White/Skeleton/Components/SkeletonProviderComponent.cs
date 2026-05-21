using Content.Shared._White.Skeleton.Systems;
using Content.Shared._White.Wounds;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Skeleton.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SkeletonSystem))]
public sealed partial class SkeletonProviderComponent : Component
{
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
