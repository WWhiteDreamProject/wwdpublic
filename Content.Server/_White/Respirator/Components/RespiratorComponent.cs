using Content.Server._White.Respirator.Systems;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Respirator.Components;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(RespiratorSystem))]
public sealed partial class RespiratorComponent : Component
{
    /// <summary>
    /// This value represents the amount of saturation lost per update.
    /// </summary>
    [DataField]
    public float Consumption;

    /// <summary>
    /// The current saturation level of the entity's respiratory system with breathable air.
    /// This is a normalized value, typically ranging from 0 (completely depleted) to 1 (fully saturated).
    /// </summary>
    [DataField]
    public float Level = 1.0f;

    /// <summary>
    /// The proportion of inhaled gas that is effectively metabolized by the entity.
    /// This value is a coefficient ranging from 0 (no metabolism) to 1 (100% metabolism).
    /// </summary>
    [DataField]
    public float Ratio = 1.0f;

    /// <summary>
    /// The maximum saturation level at which the entity will begin to experience suffocation.
    /// </summary>
    [DataField]
    public float Threshold = 0.95f;

    /// <summary>
    /// The total volume of gas that can be processed in a single breath cycle.
    /// </summary>
    [DataField]
    public float Volume;

    /// <summary>
    /// A multiplier applied to the base value <see cref="UpdateInterval"/>.
    /// The update frequency is adjusted based on the body's metabolic rate.
    /// </summary>
    [DataField]
    public float UpdateIntervalMultiplier = 1f;

    /// <summary>
    /// The <see cref="EmotePrototype"/> ID used when the entity gasps due to low oxygen.
    /// </summary>
    [DataField]
    public ProtoId<EmotePrototype> Gasp = "Gasp";

    /// <summary>
    /// The minimum time that must pass between consecutive gasping emoting.
    /// This prevents rapid, spammy gasping emote.
    /// </summary>
    [DataField]
    public TimeSpan GaspCooldown = TimeSpan.FromSeconds(8);

    /// <summary>
    /// The base interval between individual breathing operations (either inhale or exhale).
    /// A full breath cycle (inhale then exhale) will take approximately twice this duration.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Tracks the number of consecutive update cycles where the entity's saturation level has been below the <see cref="Threshold"/>.
    /// </summary>
    [ViewVariables]
    public int Suffocation = 0;

    /// <summary>
    /// The number of consecutive <see cref="Suffocation"/> required before the suffocation alert is triggered.
    /// </summary>
    [ViewVariables]
    public int SuffocationThreshold = 3;

    /// <summary>
    /// The current status of the respirator, indicating whether it is currently performing an inhale or exhale operation.
    /// </summary>
    [ViewVariables]
    public RespiratorStatus Status = RespiratorStatus.Inhaling;

    /// <summary>
    /// The actual update interval for breathing cycles, adjusted by the <see cref="UpdateIntervalMultiplier"/>.
    /// </summary>
    [ViewVariables]
    public TimeSpan CurrentUpdateInterval => UpdateInterval * UpdateIntervalMultiplier;

    /// <summary>
    /// Records the last time a gasping emote was performed. Used to enforce the <see cref="GaspCooldown"/>.
    /// </summary>
    [ViewVariables]
    public TimeSpan LastGaspTime;

    /// <summary>
    /// The scheduled time for the next breathing operation (either inhale or exhale).
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}

public enum RespiratorStatus
{
    Inhaling,
    Exhaling,
}
