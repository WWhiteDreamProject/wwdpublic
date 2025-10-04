using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.RemoteControl.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoteControllableComponent : Component
{
    [DataField]
    public EntProtoId EndRemoteControlAction = "ActionEndRemoteControl";

    [DataField]
    public bool ManualControl;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? User;

    [ViewVariables]
    public EntityUid EndRemoteControlActionUid;
}
