using Content.Shared._White.Body;
using Content.Shared._White.TargetDoll.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._White.TargetDoll.Components;

/// <summary>
/// Component that tracks which body provider is currently selected on the target doll UI.
/// Used for targeting specific body parts in medical and combat interfaces.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTargetDollSystem))]
public sealed partial class TargetDollComponent : Component
{
    /// <summary>
    /// The currently selected body provider type on the target doll.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public BodyProviderType Selected = BodyProviderType.Chest;
}
