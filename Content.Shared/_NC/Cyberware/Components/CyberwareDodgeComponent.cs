using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._NC.Cyberware.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CyberwareDodgeComponent : Component
{
    /// <summary>
    ///     Шанс уклонения от 0 до 1.
    /// </summary>
    [DataField("chance"), AutoNetworkedField]
    public float Chance = 0.15f;

    /// <summary>
    ///     Кулдаун между уклонениями в секундах.
    /// </summary>
    [DataField("cooldown"), AutoNetworkedField]
    public float Cooldown = 0.5f;

    /// <summary>
    ///     Время следующего возможного уклонения.
    /// </summary>
    [DataField("nextDodgeTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    public TimeSpan NextDodgeTime = TimeSpan.Zero;
}
