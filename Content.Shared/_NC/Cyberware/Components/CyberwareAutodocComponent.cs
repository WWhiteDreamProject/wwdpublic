using Robust.Shared.GameStates;
using Robust.Shared.Containers;

namespace Content.Shared._NC.Cyberware.Components;

/// <summary>
///     Прикрепляется к консоли Автодока. 
///     Отвечает за интерфейс (BUI) хирургии киберимплантов.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CyberwareAutodocComponent : Component
{
    public const string BodyContainerId = "autodoc-body";

    [ViewVariables]
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    ///     Время (в секундах), необходимое на интеграцию импланта.
    /// </summary>
    [DataField("installTime"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float InstallTime = 5f;

    /// <summary>
    ///     Время (в секундах), необходимое на извлечение импланта.
    /// </summary>
    [DataField("removeTime"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float RemoveTime = 8f;
}