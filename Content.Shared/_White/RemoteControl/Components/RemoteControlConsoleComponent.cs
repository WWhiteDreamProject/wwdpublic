using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.RemoteControl.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RemoteControlConsoleComponent : Component
{
    [DataField]
    public List<EntityUid> LinkedEntities = new();

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public int LastIndex;

    [DataField]
    public EntProtoId SwitchToNextAction = "RemoteControlConsoleSwitchToNextAction";

    [DataField]
    public EntityUid SwitchToNextActionUid;

    [ViewVariables]
    public EntityUid? User;

    [ViewVariables]
    public EntityUid? Target;
}
