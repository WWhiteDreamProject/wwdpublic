using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.CitiNet;

[RegisterComponent, NetworkedComponent]
public sealed partial class CitiNetNodeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("state")]
    public CitiNetNodeState State = CitiNetNodeState.Idle;

    [ViewVariables(VVAccess.ReadWrite), DataField("progress")]
    public float Progress = 0f;

    [ViewVariables(VVAccess.ReadWrite), DataField("downloadDuration")]
    public float DownloadDuration = 240f;

    [ViewVariables(VVAccess.ReadWrite), DataField("cooldownDuration")]
    public float CooldownDuration = 300f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float RemainingTime = 0f;

    [ViewVariables(VVAccess.ReadWrite), DataField("idlePower")]
    public float IdlePower = 1000f;

    [ViewVariables(VVAccess.ReadWrite), DataField("activePower")]
    public float ActivePower = 50000f;
    
    [ViewVariables(VVAccess.ReadWrite), DataField("cleanTechChance")]
    public float CleanTechChance = 0.15f;

    /// <summary>
    /// Время в секундах, необходимое для подключения носителя (DoAfter).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("connectDelay")]
    public float ConnectDelay = 2f;

    /// <summary>
    /// Время в секундах, необходимое для экстренного прерывания (DoAfter).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("emergencyDelay")]
    public float EmergencyDelay = 3f;

    /// <summary>
    /// Прототип, который спавнится при фатальной ошибке.
    /// </summary>
    [DataField("burnedDiskPrototype")]
    public string BurnedDiskPrototype = "NCRawDataBurned";

    // --- Звуки ---
    [DataField("soundConnect")]
    public SoundSpecifier SoundConnect = new SoundPathSpecifier("/Audio/Machines/terminal_insert_disc.ogg");

    [DataField("soundError")]
    public SoundSpecifier SoundError = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    [DataField("soundSuccess")]
    public SoundSpecifier SoundSuccess = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    /// <summary>
    /// Пул прототипов чистых технологий (Clean Tech) и их относительный вес (шанс выпадения внутри 15%).
    /// </summary>
    [DataField("cleanTechPool")]
    public Dictionary<string, float> CleanTechPool = new()
    {
        { "NCTechPistolBasic", 50f },
        { "NCTechRifleStandard", 30f },
        { "NCTechSniperElite", 20f }
    };

    /// <summary>
    /// Пул прототипов сырых данных (Raw Data) и их относительный вес (шанс выпадения внутри 85%).
    /// </summary>
    [DataField("rawDataPool")]
    public Dictionary<string, float> RawDataPool = new()
    {
        { "NCRawDataTier1", 100f }
    };
}
