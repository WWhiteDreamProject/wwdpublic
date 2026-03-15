using Robust.Shared.Serialization;
using Robust.Shared.Network;
using System;
using System.Collections.Generic;

namespace Content.Shared._NC.Ncpd;

[Serializable, NetSerializable]
public enum NcpdCaptainConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class NcpdCaptainConsoleBuiState : BoundUserInterfaceState
{
    public readonly int Budget;
    public readonly int InsertedCash;
    public readonly List<NcpdLogEntry> Logs;
    public readonly List<NcpdPersonnelData> Personnel;

    public NcpdCaptainConsoleBuiState(int budget, int insertedCash, List<NcpdLogEntry> logs, List<NcpdPersonnelData> personnel)
    {
        Budget = budget;
        InsertedCash = insertedCash;
        Logs = logs;
        Personnel = personnel;
    }
}

[Serializable, NetSerializable]
public struct NcpdLogEntry
{
    public TimeSpan Time;
    public string OfficerName;
    public string TargetName;
    public int Amount;
    public string Status;
    public string Reason;
}

[Serializable, NetSerializable]
public struct NcpdPersonnelData
{
    public NetEntity PlayerEntity;
    public string Name;
    public string Job;
    public bool IsSuspended;
}

[Serializable, NetSerializable]
public sealed class NcpdPurchaseMessage : BoundUserInterfaceMessage
{
    public readonly string ItemId;

    public NcpdPurchaseMessage(string itemId)
    {
        ItemId = itemId;
    }
}

[Serializable, NetSerializable]
public sealed class NcpdRevokeAccessMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity TargetEntity;

    public NcpdRevokeAccessMessage(NetEntity targetEntity)
    {
        TargetEntity = targetEntity;
    }
}

[Serializable, NetSerializable]
public sealed class NcpdClearLogsMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class NcpdWithdrawBudgetMessage : BoundUserInterfaceMessage
{
    public readonly int Amount;

    public NcpdWithdrawBudgetMessage(int amount)
    {
        Amount = amount;
    }
}

[Serializable, NetSerializable]
public sealed class NcpdDepositBudgetMessage : BoundUserInterfaceMessage
{
    public readonly int Amount;

    public NcpdDepositBudgetMessage(int amount)
    {
        Amount = amount;
    }
}
