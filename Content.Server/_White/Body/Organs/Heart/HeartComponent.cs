using Content.Shared.FixedPoint;

namespace Content.Server._White.Body.Organs.Heart;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class HeartComponent : Component
{
    /// <summary>
    /// How much damage to inflict on the heart depending on strain.
    /// </summary>
    [DataField]
    public FixedPoint2 StrainDamage = 0.1;

    /// <summary>
    /// How much chance to inflict damage on the heart depending on strain.
    /// </summary>
    [DataField]
    public float CurrentStrainDamageChanceThreshold;

    /// <summary>
    /// How much the reported heart rate deviates.
    /// </summary>
    [DataField]
    public int HeartRateDeviation = 15;

    /// <summary>
    /// Minimum heart rate.
    /// </summary>
    [DataField]
    public int MinHeartRate = 75;

    /// <summary>
    /// Maximum heart rate.
    /// </summary>
    [DataField]
    public int MaxHeartRate = 200;

    /// <summary>
    /// How much chance to inflict damage on the heart depending on strain.
    /// The highest amount is chosen.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<float, float> StrainDamageChanceThresholds;

    /// <summary>
    /// Is the heart beating now?
    /// </summary>
    [ViewVariables]
    public bool Enable;

    /// <summary>
    /// Current blood level in the body.
    /// </summary>
    [ViewVariables]
    public float BloodLevel = 1f;

    /// <summary>
    /// How badly is the heart damaged?
    /// </summary>
    [ViewVariables]
    public float HealthFactor = 1f;

    /// <summary>
    /// Body metabolic rate.
    /// </summary>
    [ViewVariables]
    public float MetabolicRate = 1f;

    /// <summary>
    /// How well does the heart pump blood around the body?
    /// </summary>
    [ViewVariables]
    public float OxygenSupply => SaturationLevel * MathF.Min(BloodLevel, HealthFactor);

    /// <summary>
    /// Current blood saturation level in the body.
    /// </summary>
    [ViewVariables]
    public float SaturationLevel = 1f;

    /// <summary>
    /// How tense is the heart at the moment?
    /// </summary>
    [ViewVariables]
    public float Strain = 0f;

    /// <summary>
    /// Current heart rate.
    /// </summary>
    [ViewVariables]
    public int HeartRate = 0;

    /// <summary>
    /// The next time that this heart will beat.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}
