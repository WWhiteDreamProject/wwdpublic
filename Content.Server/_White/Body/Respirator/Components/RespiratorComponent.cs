using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Body.Respirator.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class RespiratorComponent : Component
{
    /// <summary>
    /// How much of the gas we inhale is metabolized? Value range is (0, 1].
    /// </summary>
    [DataField]
    public float Ratio = 1.0f;

    /// <summary>
    /// Saturation level.
    /// </summary>
    [DataField]
    public float SaturationLevel = 1.0f;

    /// <summary>
    /// At what level of saturation will you begin to suffocate?
    /// </summary>
    [DataField]
    public float SuffocationThreshold = 0.95f;

    /// <summary>
    /// Multiplier applied to <see cref="UpdateInterval"/> for adjusting based on metabolic rate.
    /// </summary>
    [DataField]
    public float UpdateIntervalMultiplier = 1f;

    /// <summary>
    /// The emoting when gasps.
    /// </summary>
    [DataField]
    public ProtoId<EmotePrototype> GaspEmote = "Gasp";

    /// <summary>
    /// The interval between the gasp emotions.
    /// </summary>
    [DataField]
    public TimeSpan GaspEmoteCooldown = TimeSpan.FromSeconds(8);

    /// <summary>
    /// The interval between updates. Each update is either inhale or exhale,
    /// so a full cycle takes twice as long.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// How many cycles in a row has the mob been under-saturated?
    /// </summary>
    [ViewVariables]
    public int SuffocationCycles = 0;

    /// <summary>
    /// How many cycles in a row does it take for the suffocation alert to pop up?
    /// </summary>
    [ViewVariables]
    public int SuffocationCycleThreshold = 3;

    [ViewVariables]
    public RespiratorStatus Status = RespiratorStatus.Inhaling;

    /// <summary>
    /// Adjusted update interval based off of the multiplier value.
    /// </summary>
    [ViewVariables]
    public TimeSpan AdjustedUpdateInterval => UpdateInterval * UpdateIntervalMultiplier;

    [ViewVariables]
    public TimeSpan LastGaspEmoteTime;

    /// <summary>
    /// The next time that this body will inhale or exhale.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}

public enum RespiratorStatus
{
    Inhaling,
    Exhaling
}
