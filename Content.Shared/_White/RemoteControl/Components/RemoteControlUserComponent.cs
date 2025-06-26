using Robust.Shared.GameStates;

namespace Content.Shared._White.RemoteControl.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoteControlUserComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid Target;

    [ViewVariables, AutoNetworkedField]
    public EntityUid Console;
}
