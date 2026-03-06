using Content.Shared.CartridgeLoader;
using Content.Shared._NC.CitiNet.Live;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.CitiNet;

// ========== Enums ==========

/// <summary>
/// Состояние звонка в CitiNet.
/// </summary>
[Serializable, NetSerializable]
public enum CitiNetCallState : byte
{
    None,
    Ringing,    // Исходящий вызов, ожидаем ответа
    Incoming,   // Входящий вызов, ждём принятия
    Active      // Разговор идёт
}

/// <summary>
/// Тип UI-сообщения от клиента к серверу.
/// </summary>
[Serializable, NetSerializable]
public enum CitiNetUiMessageType : byte
{
    // P2P звонки
    StartChat,     // Открывает текстовый чат с агентом
    CloseChat,     // Закрывает текстовый чат
    InitiateCall,  // Начинает голосовой вызов в открытом чате
    AcceptCall,
    DeclineCall,
    HangUp,
    SendCallMessage,
    PingLocation,

    // Групповые звонки
    CreateGroup,
    InviteToGroup,
    LeaveGroup,
    SendGroupMessage,
    JoinGroupVoice,   // Подключиться к голосовому Relay в тактическом мосте
    LeaveGroupVoice,  // Отключиться от голосового Relay

    // BBS
    JoinChannel,
    LeaveChannel,
    SendBBSMessage,
    SelectChannel
}

// ========== UI Message (Client → Server) ==========

/// <summary>
/// Универсальное UI-сообщение картриджа CitiNet.
/// Передаётся через CartridgeLoader.
/// </summary>
[Serializable, NetSerializable]
public sealed class CitiNetUiMessageEvent : CartridgeMessageEvent
{
    public readonly CitiNetUiMessageType Type;

    /// <summary>
    /// Номер цели (для звонков), ID канала (для BBS), или null.
    /// </summary>
    public readonly string? TargetId;

    /// <summary>
    /// Текст сообщения или пароль.
    /// </summary>
    public readonly string? Content;

    public CitiNetUiMessageEvent(CitiNetUiMessageType type, string? targetId = null, string? content = null)
    {
        Type = type;
        TargetId = targetId;
        Content = content;
    }
}

/// <summary>
/// NC — Сообщение из вкладки LIVE.
/// Передаётся через CartridgeLoader отдельным типом.
/// </summary>
[Serializable, NetSerializable]
public sealed class CitiNetLiveMessageEvent : CartridgeMessageEvent
{
    public readonly CitiNetLiveMessageType Type;

    /// <summary>
    /// Содержимое: название стрима, NetEntity строка, "amount|msg" для доната, текст чата.
    /// </summary>
    public readonly string? Content;

    public CitiNetLiveMessageEvent(CitiNetLiveMessageType type, string? content = null)
    {
        Type = type;
        Content = content;
    }
}

// ========== UI State (Server → Client) ==========

/// <summary>
/// Сообщение в BBS-канале.
/// </summary>
[Serializable, NetSerializable, DataRecord]
public struct CitiNetBBSMessage
{
    public const int MaxContentLength = 256;

    public TimeSpan Timestamp;
    public string SenderName;   // Имя отправителя (или "Аноним" для анонимных каналов)
    public string Content;
    public string ChannelId;

    public CitiNetBBSMessage(TimeSpan timestamp, string senderName, string content, string channelId)
    {
        Timestamp = timestamp;
        SenderName = senderName;
        Content = content;
        ChannelId = channelId;
    }
}

/// <summary>
/// Сообщение в звонке (P2P или группа).
/// </summary>
[Serializable, NetSerializable, DataRecord]
public struct CitiNetCallMessage
{
    public const int MaxContentLength = 256;

    public TimeSpan Timestamp;
    public string SenderName;
    public string Content;
    public bool IsSystem;   // Системное уведомление (FLATLINE и т.д.)

    public CitiNetCallMessage(TimeSpan timestamp, string senderName, string content, bool isSystem = false)
    {
        Timestamp = timestamp;
        SenderName = senderName;
        Content = content;
        IsSystem = isSystem;
    }
}

/// <summary>
/// Информация о BBS-канале для UI.
/// </summary>
[Serializable, NetSerializable, DataRecord]
public struct CitiNetChannelInfo
{
    public string Id;
    public string Name;
    public Color Color;
    public bool RequiresPassword;
    public bool IsJoined;

    public CitiNetChannelInfo(string id, string name, Color color, bool requiresPassword, bool isJoined)
    {
        Id = id;
        Name = name;
        Color = color;
        RequiresPassword = requiresPassword;
        IsJoined = isJoined;
    }
}

/// <summary>
/// Информация об участнике группового звонка.
/// </summary>
[Serializable, NetSerializable, DataRecord]
public struct CitiNetGroupParticipant
{
    public string Name;
    public bool IsAlive;

    public CitiNetGroupParticipant(string name, bool isAlive = true)
    {
        Name = name;
        IsAlive = isAlive;
    }
}

/// <summary>
/// Информация о контакте P2P (открытый чат).
/// </summary>
[Serializable, NetSerializable, DataRecord]
public struct CitiNetContact
{
    public string Number;
    public string Name;

    public CitiNetContact(string number, string name)
    {
        Number = number;
        Name = name;
    }
}

/// <summary>
/// Полное состояние UI картриджа CitiNet.
/// </summary>
[Serializable, NetSerializable]
public sealed class CitiNetUiState : BoundUserInterfaceState
{
    // Общее
    public readonly string OwnNumber;           // Номер этого Агента
    public readonly bool HasRelay;              // Есть ли активный CitiNet Relay

    // P2P звонки и чаты
    public readonly List<CitiNetContact> Contacts;          // Список открытых P2P чатов
    public readonly string? CurrentContactNumber;           // С кем сейчас открыто окно чата
    public readonly CitiNetCallState CallState;             // Состояние голосового вызова (к CurrentContactNumber)
    public readonly List<CitiNetCallMessage> CallMessages;  // Сообщения с CurrentContactNumber

    // Групповые звонки
    public readonly bool InGroup;
    public readonly bool InGroupVoice;              // Включена ли трансляция голоса
    public readonly List<CitiNetGroupParticipant> GroupParticipants;
    public readonly int MaxGroupParticipants;
    public readonly List<CitiNetCallMessage> GroupMessages;

    // BBS
    public readonly List<CitiNetChannelInfo> Channels;
    public readonly string? CurrentChannelId;
    public readonly List<CitiNetBBSMessage> ChannelMessages;


    public CitiNetUiState(
        string ownNumber,
        bool hasRelay,
        List<CitiNetContact> contacts,
        string? currentContactNumber,
        CitiNetCallState callState,
        List<CitiNetCallMessage> callMessages,
        bool inGroup,
        bool inGroupVoice,
        List<CitiNetGroupParticipant> groupParticipants,
        int maxGroupParticipants,
        List<CitiNetCallMessage> groupMessages,
        List<CitiNetChannelInfo> channels,
        string? currentChannelId,
        List<CitiNetBBSMessage> channelMessages)
    {
        OwnNumber = ownNumber;
        HasRelay = hasRelay;
        Contacts = contacts;
        CurrentContactNumber = currentContactNumber;
        CallState = callState;
        CallMessages = callMessages;
        InGroup = inGroup;
        InGroupVoice = inGroupVoice;
        GroupParticipants = groupParticipants;
        MaxGroupParticipants = maxGroupParticipants;
        GroupMessages = groupMessages;
        Channels = channels;
        CurrentChannelId = currentChannelId;
        ChannelMessages = channelMessages;
    }
}
