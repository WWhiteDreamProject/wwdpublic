using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Chemistry.Components;

/// <summary>
/// NC: Компонент для отслеживания временных эффектов реагентов (баффы, таймеры шансов).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NCReagentEffectsComponent : Component
{
    /// <summary>
    /// Время, до которого активно усиленное лечение Прайм Тайма.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PrimeTimeHealingUntil;

    /// <summary>
    /// Время, до которого активно лечение Берсерка.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BerserkHealingUntil;

    /// <summary>
    /// Время последнего "броска кубика" на конечности (чтобы не спамить чаще 1 раза в сек).
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan LastLimbCheckTime;
}
