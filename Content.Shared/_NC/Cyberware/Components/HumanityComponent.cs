using Robust.Shared.GameStates;

namespace Content.Shared._NC.Cyberware.Components;

/// <summary>
///     Хранит запас рассудка (человечности) персонажа.
///     Снижается при установке имплантов.
///     Максимальное значение необратимо падает при удалении.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HumanityComponent : Component
{
    /// <summary>
    ///     Текущее значение человечности. (По умолчанию 100)
    /// </summary>
    [DataField("currentHumanity"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float CurrentHumanity = 100f;

    /// <summary>
    ///     Максимальное значение человечности. Падает при удалении импланта (перманентная травма).
    /// </summary>
    [DataField("maxHumanity"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float MaxHumanity = 100f;

    /// <summary>
    ///     Базовое (изначальное) значение. Может в будущем масштабироваться от Эмпатии (Эмпатия * 10).
    /// </summary>
    [DataField("baseHumanity"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float BaseHumanity = 100f;

    /// <summary>
    ///     Список слов, которые лечат пациента в текущем сеансе.
    /// </summary>
    [DataField("targetHealingWords"), ViewVariables(VVAccess.ReadWrite)]
    public List<string> TargetHealingWords = new();

    /// <summary>
    ///     Слова, которые вызывают болевую реакцию.
    /// </summary>
    [DataField("targetTraumaWords"), ViewVariables(VVAccess.ReadWrite)]
    public List<string> TargetTraumaWords = new();

    /// <summary>
    ///     Активный биомонитор, подключённый к пациенту.
    /// </summary>
    [DataField("activeBiomonitor"), ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ActiveBiomonitor;
}
