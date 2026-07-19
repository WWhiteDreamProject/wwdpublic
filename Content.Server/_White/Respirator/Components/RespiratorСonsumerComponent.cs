using Content.Server._White.Respirator.Systems;
using Content.Shared._White.Damage;

namespace Content.Server._White.Respirator.Components;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(RespiratorSystem))]
public sealed partial class RespiratorСonsumerComponent : Component
{
    /// <summary>
    /// Represents the rate at which this consumer depletes the body oxidant saturation level.
    /// </summary>
    [DataField]
    public float Consumption = 0.01f;

    /// <summary>
    /// The amount of damage by this consumer during each update when the body oxygenate levels are critically low.
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new ("Bloodloss", 0.1);

    /// <summary>
    /// The minimum percentage of available oxidant in the body required to avoid incurring damage from low oxygenate levels.
    /// </summary>
    [DataField]
    public float DamageThreshold = 0.5f;

    /// <summary>
    /// The interval between update for this consumer
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Current blood level in the body.
    /// </summary>
    [ViewVariables]
    public float BloodLevel = 1f;

    /// <summary>
    /// The metabolic rate of the body.
    /// </summary>
    [ViewVariables]
    public float MetabolicRate = 1f;

    /// <summary>
    /// The current saturation level of the body.
    /// </summary>
    [ViewVariables]
    public float SaturationLevel = 1f;

    /// <summary>
    /// The scheduled time for the next update of this consumer.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}
