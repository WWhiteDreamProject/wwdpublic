using Content.Shared._NC.Netrunning.Components;
using Robust.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Shared._NC.Netrunning.UI;

[Serializable, NetSerializable]
public enum NetServerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public struct NetDeviceData
{
    public string Name;
    public string ProtoId;
    public bool IsPowered;

    public NetDeviceData(string name, string protoId, bool isPowered)
    {
        Name = name;
        ProtoId = protoId;
        IsPowered = isPowered;
    }
}

[Serializable, NetSerializable]
public struct NetServerSlotData
{
    public string SlotName;
    public string SlotLabel; // Friendly name (e.g., "Floor 1")
    public string? ItemName;
    public NetIceType? IceType;
    public bool HasIce;
    public string InitPassword;

    public NetServerSlotData(string slotName, string slotLabel, string? itemName, NetIceType? iceType, bool hasIce, string initPassword)
    {
        SlotName = slotName;
        SlotLabel = slotLabel;
        ItemName = itemName;
        IceType = iceType;
        HasIce = hasIce;
        InitPassword = initPassword;
    }
}

[Serializable, NetSerializable]
public sealed class NetServerBoundUiState : BoundUserInterfaceState
{
    public bool HasApc { get; }
    public bool IsPowered { get; }
    public List<NetDeviceData> Devices { get; }
    public string ServerName { get; }
    public List<NetServerSlotData> Slots { get; }

    public NetServerBoundUiState(bool hasApc, bool isPowered, List<NetDeviceData> devices, string serverName, List<NetServerSlotData> slots)
    {
        HasApc = hasApc;
        IsPowered = isPowered;
        Devices = devices;
        ServerName = serverName;
        Slots = slots;
    }
}

[Serializable, NetSerializable]
public sealed class NetServerSetPasswordMessage : BoundUserInterfaceMessage
{
    public string SlotName { get; }
    public string Password { get; }

    public NetServerSetPasswordMessage(string slotName, string password)
    {
        SlotName = slotName;
        Password = password;
    }
}
