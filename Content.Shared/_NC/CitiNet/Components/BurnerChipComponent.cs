using Robust.Shared.GameStates;

namespace Content.Shared._NC.CitiNet.Components;

/// <summary>
/// Одноразовый чип анонимности (Burner Chip).
/// Вставляется в PDA для подмены OwnerName на временный анонимный ID.
/// После извлечения помечается как использованный и не может быть использован повторно.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BurnerChipComponent : Component
{
    /// <summary>
    /// Сгенерированный временный анонимный ID.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string TempId = string.Empty;

    /// <summary>
    /// Был ли чип уже использован.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsUsed;

    /// <summary>
    /// Оригинальное имя владельца PDA до подмены (для восстановления).
    /// </summary>
    [ViewVariables]
    public string? OriginalOwnerName;

    /// <summary>
    /// UID PDA, в который вставлен чип.
    /// </summary>
    [ViewVariables]
    public EntityUid? InsertedInto;
}
