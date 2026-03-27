using Robust.Shared.GameStates;

namespace Content.Shared._NC.Cyberware.Components;

/// <summary>
///     Компонент-маркер для отслеживания состояния киберпсихоза и таймеров эффектов.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CyberpsychosisComponent : Component
{
    /// <summary>
    ///     Таймер до следующего случайного сбоя (для Стадии 2).
    /// </summary>
    [DataField("glitchTimer"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float GlitchTimer = 0f;
}