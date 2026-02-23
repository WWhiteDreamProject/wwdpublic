using Content.Shared._White.Body.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._White.TargetDoll;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTargetDollSystem))]
public sealed partial class TargetDollComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public BodyPartType SelectedBodyPartType = BodyPartType.Chest;
}
