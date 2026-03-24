using Robust.Shared.Serialization;

namespace Content.Shared._NC.Citation;

/// <summary>
/// Компонент для устройства "Полицейский терминал штрафов" (NCPD Citation Terminal).
/// </summary>
[RegisterComponent]
public sealed partial class CitationDeviceComponent : Component
{
    /// <summary>
    /// Активный подозреваемый, которого штрафуют прямо сейчас.
    /// </summary>
    [DataField("activeTarget")]
    public EntityUid? ActiveTarget;

    /// <summary>
    /// Офицер, который держит терминал и инициировал штраф.
    /// </summary>
    [DataField("activeOfficer")]
    public EntityUid? ActiveOfficer;

    /// <summary>
    /// ID-карта, по которой кликнули для принудительного штрафа.
    /// </summary>
    [DataField("activeIdCard")]
    public EntityUid? ActiveIdCard;

    /// <summary>
    /// Запрашиваемая сумма штрафа.
    /// </summary>
    [DataField("requestedAmount")]
    public int RequestedAmount = 0;

    /// <summary>
    /// Причина штрафа.
    /// </summary>
    [DataField("reason")]
    public string Reason = string.Empty;

    /// <summary>
    /// Максимальный лимит для обычного копа (если не детектив и не кэп).
    /// </summary>
    [DataField("patrolLimit")]
    public int PatrolLimit = 500;

    /// <summary>
    /// Максимальный лимит для детектива/лейтенанта.
    /// </summary>
    [DataField("detectiveLimit")]
    public int DetectiveLimit = 1500;
    
    /// <summary>
    /// Максимальный лимит для капитана.
    /// </summary>
    [DataField("captainLimit")]
    public int CaptainLimit = 1000;

    /// <summary>
    /// Процент комиссии (легализованная взятка), который идет офицеру. (Например, 0.3f = 30%)
    /// </summary>
    [DataField("commissionRate")]
    public float CommissionRate = 0.3f;
}
