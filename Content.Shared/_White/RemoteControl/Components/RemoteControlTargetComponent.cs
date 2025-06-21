using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.RemoteControl.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoteControlTargetComponent : Component
{
    [DataField]
    public EntProtoId EndRemoteControlAction = "ActionEndRemoteControl";

    [DataField]
    public bool CanManually;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? User;

    [ViewVariables]
    public EntityUid EndRemoteControlActionUid;
}
