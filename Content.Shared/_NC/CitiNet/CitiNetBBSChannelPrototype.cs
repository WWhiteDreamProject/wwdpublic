using Robust.Shared.Prototypes;

namespace Content.Shared._NC.CitiNet;

/// <summary>
/// Прототип BBS-канала CitiNet.
/// Определяет гражданские текстовые каналы, доступные через PDA.
/// </summary>
[Prototype("citiNetChannel")]
public sealed partial class CitiNetBBSChannelPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Локализованное название канала.
    /// </summary>
    [DataField]
    public LocId Name { get; private set; } = string.Empty;

    [ViewVariables]
    public string LocalizedName => Loc.GetString(Name);

    /// <summary>
    /// Цвет канала в UI и чате.
    /// </summary>
    [DataField]
    public Color Color { get; private set; } = Color.Gray;

    /// <summary>
    /// Требуется ли пароль для доступа к каналу (банд-каналы).
    /// </summary>
    [DataField]
    public bool RequiresPassword { get; private set; }

    /// <summary>
    /// Пароль для доступа (если RequiresPassword == true).
    /// </summary>
    [DataField]
    public string? Password { get; private set; }

    /// <summary>
    /// Является ли канал анонимным (общегородской).
    /// В анонимных каналах имя отправителя скрыто.
    /// </summary>
    [DataField]
    public bool IsAnonymous { get; private set; }

    /// <summary>
    /// Скрыт ли канал из списка по умолчанию (виден только если к нему уже присоединились).
    /// </summary>
    [DataField]
    public bool IsHidden { get; private set; }
}
