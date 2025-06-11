using Content.Shared.Actions;
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
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? ControllingMind;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? ControllingEntity;

    [DataField]
    public bool EnsureSinkPort = true;
    
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
    public List<EntityUid> LinkedEntities = new();

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public int LastIndex;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? CurrentUser;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? CurrentEntity;

    [DataField]
    public EntProtoId SwitchToNextAction = "RemoteControlConsoleSwitchToNextAction";
    [DataField]
    public EntityUid? SwitchToNextActionEntity;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? ControllingEntity;
}

[NetworkedComponent]
[RegisterComponent]
public sealed partial class ManuallyControllableComponent : Component
{
    [DataField]
    public bool Enabled = true;
    public EntityUid? CurrentUser;
}

public sealed partial class RemoteControlConsoleSwitchNextActionEvent : InstantActionEvent;
public sealed partial class RemoteControlExitActionEvent : InstantActionEvent;
