using Content.Shared._NC.CitiNet.Live;

namespace Content.Server._NC.CitiNet.Live;

/// <summary>
/// Компонент нагрудной камеры CitiNet Live.
/// Вешается на StreamCamNC. Управляется CitiNetStreamSystem.
/// </summary>
[RegisterComponent]
public sealed partial class StreamCamComponent : Component
{
    /// <summary>Активна ли трансляция в данный момент.</summary>
    [DataField]
    public bool IsStreaming;

    /// <summary>Название стрима (задаётся игроком при включении).</summary>
    [DataField]
    public string StreamTitle = string.Empty;

    /// <summary>Требуется ли наличие заряженной батареи для работы стрима.</summary>
    [DataField]
    public bool RequireBattery = true;

    /// <summary>Расход энергии батарейки в единицах за 1 секунду работы.</summary>
    [DataField]
    public float EnergyDrainRate = 5f;

    /// <summary>Текущее количество зрителей.</summary>
    public int ViewerCount;

    /// <summary>История чата (очищается при остановке стрима).</summary>
    public List<LiveChatMessage> ChatMessages = new();

    /// <summary>Максимальная длина истории чата (старые сообщения обрезаются).</summary>
    [DataField]
    public int MaxChatMessages = 100;

    /// <summary>EntityUid владельца (актора, который несёт/надел камеру). Проставляется системой.</summary>
    public EntityUid? HolderUid;
}
