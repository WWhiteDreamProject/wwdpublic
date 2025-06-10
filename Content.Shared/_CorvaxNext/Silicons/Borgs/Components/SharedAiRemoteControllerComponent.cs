using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

```suggestion
namespace Content.Shared._White.Silicons.Borgs.Components;

[RegisterComponent]
public sealed partial class AiRemoteControllerComponent : Component
{
    [DataField]
    public EntProtoId BackToAiAction = "ActionBackToAi";

    [ViewVariables]
    public EntityUid? BackToAiActionEntity;

    [ViewVariables]
    public EntityUid? AiHolder;

    [ViewVariables]
    public EntityUid? LinkedMind;

    [ViewVariables]
    public string[]? PreviouslyTransmitterChannels;

    [ViewVariables]
    public string[]? PreviouslyActiveRadioChannels;
}

[Serializable, NetSerializable]
public sealed class RemoteDeviceActionMessage : BoundUserInterfaceMessage
{
    public readonly RemoteDeviceActionEvent? RemoteAction;

    public RemoteDeviceActionMessage(RemoteDeviceActionEvent remoteDeviceAction)
    {
        RemoteAction = remoteDeviceAction;
    }
}

[Serializable, NetSerializable]
public sealed class RemoteDeviceActionEvent : EntityEventArgs
{
    public RemoteDeviceActionType ActionType;
    public NetEntity Target;

    public RemoteDeviceActionEvent(RemoteDeviceActionType actionType, NetEntity target)
    {
        ActionType = actionType;
        Target = target;
    }
}

[Serializable, NetSerializable]
public record struct RemoteDevicesData()
{
    public string DisplayName = string.Empty;

    public NetEntity NetEntityUid = NetEntity.Invalid;
}

[Serializable, NetSerializable]
public sealed class RemoteDevicesBuiState : BoundUserInterfaceState
{
    public List<RemoteDevicesData> DeviceList;

    public RemoteDevicesBuiState(List<RemoteDevicesData> deviceList)
    {
        DeviceList = deviceList;
    }
}

public enum RemoteDeviceActionType
{
    MoveToDevice,
    TakeControl
}
