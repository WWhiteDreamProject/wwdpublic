using Content.Shared.DeviceLinking;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.RemoteControl;
[NetworkedComponent]
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class RemoteControllableComponent : Component
{
    //[DataField, AutoNetworkedField]
    //public bool ControllableViaVerb = false;

    [DataField, AutoNetworkedField]
    public bool ControllableViaConnection = true;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? ControllingMind;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? ControllingEntity;

    [DataField]
    public string EndRemoteControlAction = "ActionEndRemoteControl";

    [DataField, AutoNetworkedField]
    public EntityUid? EndRemoteControlActionEntity;
}

[NetworkedComponent]
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class RemoteControllingComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid ControlledEntity;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? UsedInterface;
}


[NetworkedComponent]
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class RemoteControlConsoleComponent : Component
{
    [DataField]
    public ProtoId<SourcePortPrototype> ConnectionPortId = "RemoteControlPort";

    [DataField]
    public List<EntityUid> LinkedEntities = new();

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public int LastIndex;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? CurrentUser;

    [DataField]
    public EntProtoId SwitchToNextAction = "RemoteControlConsoleSwitchToNextAction";
    [DataField]
    public EntProtoId SwitchToPreviousAction = "RemoteControlConsoleSwitchToPreviousAction";

    [DataField]
    public EntityUid? SwitchToNextActionEntity;
    [DataField]
    public EntityUid? SwitchToPreviousActionEntity;


    //[ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    //public EntityUid? ControlledEntity;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? ControllingEntity;
}
