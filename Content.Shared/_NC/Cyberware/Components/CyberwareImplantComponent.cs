using Robust.Shared.GameStates;

namespace Content.Shared._NC.Cyberware.Components;

/// <summary>
///     Определяет сущность (Item) как киберимплант, который можно интегрировать через Автодок.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CyberwareImplantComponent : Component
{
    /// <summary>
    ///     Категория анатомической зоны, куда устанавливается имплант.
    ///     Конкретный слот в категории определяется автоматически при установке.
    /// </summary>
    [DataField("category", required: true), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public CyberwareCategory Category = CyberwareCategory.None;

    /// <summary>
    ///     Количество очков человечности, которое спишется при установке импланта.
    /// </summary>
    [DataField("humanityCost"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float HumanityCost = 14f;

    /// <summary>
    ///     Процент возврата человечности при ИЗВЛЕЧЕНИИ импланта. (0.5 = 50%)
    ///     Остальная часть отнимается от Максимальной Человечности навсегда.
    /// </summary>
    [DataField("refundPercentage"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float RefundPercentage = 0.5f;

    /// <summary>
    ///     Семейство импланта. Не позволяет устанавливать несколько имплантов одного семейства.
    /// </summary>
    [DataField("cyberFamily"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public string? CyberFamily;
}