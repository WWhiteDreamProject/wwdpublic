using Robust.Shared.Serialization;

namespace Content.Shared._NC.CitiNet.Live;

// NC — CitiNet Live: общие типы для взаимодействия клиента и сервера.

/// <summary>
/// Типы сообщений от клиента к серверу через CartridgeLoader.
/// </summary>
public enum CitiNetLiveMessageType : byte
{
    StartStream,    // начать трансляцию (title = название)
    StopStream,     // остановить трансляцию
    WatchStream,    // начать просмотр (target = NetEntity камеры)
    StopWatching,   // прекратить просмотр
    SendDonate,     // отправить донат (target = NetEntity камеры, content = "amount|message")
    SendChat,       // отправить сообщение в чат стрима (content)
}

/// <summary>
/// Информация об одном активном стриме (для списка в UI зрителя).
/// </summary>
[Serializable, NetSerializable]
public sealed record StreamInfo(
    NetEntity CamNetEntity,
    string Title,
    string StreamerName,
    int ViewerCount);

/// <summary>
/// Одно сообщение чата стрима.
/// </summary>
[Serializable, NetSerializable]
public sealed record LiveChatMessage(
    TimeSpan Time,
    string Sender,
    string Text,
    bool IsSystem);

/// <summary>
/// Полное состояние вкладки LIVE, отправляемое клиенту через CartridgeLoader.
/// </summary>
[Serializable, NetSerializable]
public sealed class CitiNetLiveUiState : BoundUserInterfaceState
{
    /// <summary>Есть ли активная камера в руках или надето</summary>
    public bool HasCamera;

    /// <summary>Идёт ли трансляция с камеры этого игрока</summary>
    public bool IsStreaming;

    /// <summary>Количество зрителей своего стрима</summary>
    public int ViewerCount;

    /// <summary>Название своего стрима</summary>
    public string StreamTitle = string.Empty;

    /// <summary>Баланс банковского счёта (для UI доната)</summary>
    public int Balance;

    /// <summary>Процент заряда батареи камеры (0-100)</summary>
    public int BatteryPercent;

    /// <summary>Список всех активных стримов на сервере</summary>
    public List<StreamInfo> ActiveStreams = new();

    /// <summary>Текущий просмотр — NetEntity камеры, или null если не смотрим</summary>
    public NetEntity? WatchedCamNetEntity;

    /// <summary>Имя стримера, которого смотрим</summary>
    public string WatchedStreamerName = string.Empty;

    /// <summary>Сообщения чата наблюдаемого стрима (или своего)</summary>
    public List<LiveChatMessage> ChatMessages = new();
}
