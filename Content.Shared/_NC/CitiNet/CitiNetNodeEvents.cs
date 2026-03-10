using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.CitiNet;

[Serializable, NetSerializable]
public enum CitiNetNodeState : byte
{
    Idle,           // Ожидание, потребление 1кВт
    Downloading,    // Активное скачивание, 50кВт
    Cooldown        // Перезагрузка после успеха/сбоя
}

[Serializable, NetSerializable]
public enum CitiNetNodeVisuals : byte
{
    State,
    Progress
}

[Serializable, NetSerializable]
public enum CitiNetNodeUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CitiNetNodeEmergencyExtractionMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed partial class CitiNetNodeConnectDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class CitiNetNodeEmergencyDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class CitiNetNodeBoundUserInterfaceState : BoundUserInterfaceState
{
    public CitiNetNodeState State { get; }
    public float Progress { get; }
    public float RemainingCooldown { get; }
    public bool IsPowered { get; }

    public CitiNetNodeBoundUserInterfaceState(CitiNetNodeState state, float progress, float remainingCooldown, bool isPowered)
    {
        State = state;
        Progress = progress;
        RemainingCooldown = remainingCooldown;
        IsPowered = isPowered;
    }
}
