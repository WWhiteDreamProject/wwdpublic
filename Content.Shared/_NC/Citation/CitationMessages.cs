using Robust.Shared.Serialization;

namespace Content.Shared._NC.Citation;

/// <summary>
/// Ключи для BUI Терминала Штрафов.
/// </summary>
[Serializable, NetSerializable]
public enum CitationDeviceUiKey : byte
{
    Key
}

/// <summary>
/// Состояние UI Терминала Штрафов (для Копа).
/// </summary>
[Serializable, NetSerializable]
public sealed class CitationDeviceBuiState : BoundUserInterfaceState
{
    public readonly string TargetName;
    public readonly int MaxLimit;
    public readonly int Budget;
    public readonly bool TargetActive; // Идет ли сейчас процесс ожидания ответа

    public CitationDeviceBuiState(string targetName, int maxLimit, int budget, bool targetActive)
    {
        TargetName = targetName;
        MaxLimit = maxLimit;
        Budget = budget;
        TargetActive = targetActive;
    }
}

/// <summary>
/// Сообщение от копа (BUI): выписать штраф на указанную сумму.
/// </summary>
[Serializable, NetSerializable]
public sealed class CitationDeviceCreateMessage : BoundUserInterfaceMessage
{
    public readonly int Amount;
    public readonly string Reason;

    public CitationDeviceCreateMessage(int amount, string reason)
    {
        Amount = amount;
        Reason = reason;
    }
}

/// <summary>
/// NetMessage: Уведомление подозреваемому (Alert Dialog) о выписанном штрафе.
/// </summary>
[Serializable, NetSerializable]
public sealed class CitationTargetUiMessage : EntityEventArgs
{
    public readonly NetEntity TerminalUid;
    public readonly string OfficerName;
    public readonly int Amount;
    public readonly string Reason;

    public CitationTargetUiMessage(NetEntity terminalUid, string officerName, int amount, string reason)
    {
        TerminalUid = terminalUid;
        OfficerName = officerName;
        Amount = amount;
        Reason = reason;
    }
}

/// <summary>
/// NetMessage: Ответ подозреваемого на штраф (Оплатить / Отказаться).
/// </summary>
[Serializable, NetSerializable]
public sealed class CitationTargetResponseMessage : EntityEventArgs
{
    public readonly NetEntity TerminalUid;
    public readonly bool Accept;

    public CitationTargetResponseMessage(NetEntity terminalUid, bool accept)
    {
        TerminalUid = terminalUid;
        Accept = accept;
    }
}
