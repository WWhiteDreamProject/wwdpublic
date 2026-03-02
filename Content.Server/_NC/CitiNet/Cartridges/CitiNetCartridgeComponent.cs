namespace Content.Server._NC.CitiNet.Cartridges;

/// <summary>
/// Серверный компонент картриджа CitiNet.
/// Хранит состояние звонков, группового моста и BBS-каналов.
/// </summary>
[RegisterComponent, Access(typeof(CitiNetCartridgeSystem))]
public sealed partial class CitiNetCartridgeComponent : Component
{
    // ========== P2P Звонки ==========

    /// <summary>
    /// UID PDA-загрузчика (owner PDA).
    /// </summary>
    [ViewVariables]
    public EntityUid? LoaderUid;

    /// <summary>
    /// Текущий открытый P2P чат: EntityUid PDA собеседника.
    /// Голосовой вызов также привязывается к этому чату.
    /// </summary>
    [ViewVariables]
    public EntityUid? ActiveChatTarget;

    /// <summary>
    /// Состояние текущего звонка.
    /// </summary>
    [ViewVariables]
    public Content.Shared._NC.CitiNet.CitiNetCallState CallState =
        Content.Shared._NC.CitiNet.CitiNetCallState.None;

    /// <summary>
    /// Кто звонит нам (для входящих вызовов).
    /// </summary>
    [ViewVariables]
    public EntityUid? IncomingCaller;

    /// <summary>
    /// История сообщений P2P-чатов (Key = EntityUid собеседника).
    /// </summary>
    [ViewVariables]
    public Dictionary<EntityUid, List<Content.Shared._NC.CitiNet.CitiNetCallMessage>> ChatHistories = new();

    /// <summary>
    /// Максимум сообщений в кеше на один P2P чат.
    /// </summary>
    [DataField]
    public int MaxMessagesPerChat = 50;

    // ========== Групповые звонки ==========

    /// <summary>
    /// Находится ли этот Агент в групповом чате.
    /// </summary>
    [ViewVariables]
    public bool InGroup;

    /// <summary>
    /// Ретранслируется ли голос этого Агента в группу (и слышит ли он остальных).
    /// </summary>
    [ViewVariables]
    public bool InGroupVoice;

    /// <summary>
    /// UID'ы PDA всех участников групппы (включая себя).
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> GroupMembers = new();

    /// <summary>
    /// Максимальное количество участников группового звонка.
    /// 3 для дешёвых Агентов, 10 для корпоративных.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxGroupParticipants = 3;

    /// <summary>
    /// История сообщений группового звонка.
    /// </summary>
    [ViewVariables]
    public List<Content.Shared._NC.CitiNet.CitiNetCallMessage> GroupMessages = new();

    // ========== BBS-каналы ==========

    /// <summary>
    /// ID каналов, к которым подключён этот Агент.
    /// </summary>
    [ViewVariables]
    public HashSet<string> JoinedChannels = new();

    /// <summary>
    /// Текущий выбранный BBS-канал в UI.
    /// </summary>
    [ViewVariables]
    public string? CurrentChannel;

    /// <summary>
    /// Кеш сообщений по каналам.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, List<Content.Shared._NC.CitiNet.CitiNetBBSMessage>> ChannelMessages = new();

    /// <summary>
    /// Максимум сообщений в кеше на канал.
    /// </summary>
    [DataField]
    public int MaxMessagesPerChannel = 100;

    // ========== Номер Агента ==========

    /// <summary>
    /// Уникальный номер этого Агента для звонков.
    /// Генерируется при первом запуске.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string AgentNumber = string.Empty;
}
