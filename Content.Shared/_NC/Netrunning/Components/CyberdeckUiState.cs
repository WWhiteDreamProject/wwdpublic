using Robust.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Shared._NC.Netrunning.Components;

[Serializable, NetSerializable]
public struct NetProgramData
{
    public NetProgramType ProgramType;
    public int RamCost;
    public int EnergyCost;

    public NetProgramData(NetProgramType type, int ram, int energy)
    {
        ProgramType = type;
        RamCost = ram;
        EnergyCost = energy;
    }
}

[Serializable, NetSerializable]
public sealed class CyberdeckBoundUiState : BoundUserInterfaceState
{
    public int CurrentRam;
    public int MaxRam;
    public Dictionary<NetEntity, NetProgramData> Programs;
    public NetEntity? TargetId;
    public string? TargetName;

    public Dictionary<NetEntity, string> NearbyDevices;

    public CyberdeckBoundUiState(int currentRam, int maxRam, Dictionary<NetEntity, NetProgramData> programs, NetEntity? targetId, string? targetName, Dictionary<NetEntity, string> nearbyDevices)
    {
        CurrentRam = currentRam;
        MaxRam = maxRam;
        Programs = programs;
        TargetId = targetId;
        TargetName = targetName;
        NearbyDevices = nearbyDevices;
    }
}

[Serializable, NetSerializable]
public sealed class CyberdeckProgramRequestMessage : BoundUserInterfaceMessage
{
    public NetEntity ProgramId;

    public CyberdeckProgramRequestMessage(NetEntity programId)
    {
        ProgramId = programId;
    }
}

[Serializable, NetSerializable]
public sealed class CyberdeckSetTargetMessage : BoundUserInterfaceMessage
{
    public NetEntity TargetId;

    public CyberdeckSetTargetMessage(NetEntity targetId)
    {
        TargetId = targetId;
    }
}


