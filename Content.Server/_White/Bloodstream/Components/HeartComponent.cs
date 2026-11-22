using Content.Shared._White.Wounds;

namespace Content.Server._White.Bloodstream.Components;

/// <summary>
/// The heart rate is driven by a resting rate plus a "sympathetic drive" built from physiological stressors (blood loss, hypoxia, pain, heart damage).
/// Severe blood loss or hypoxia make the heart decompensate, which can end in a cardiac arrest.
/// The heart's rate also determines the body's metabolic rate.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class HeartComponent : Component
{
    #region Blood

    /// <summary>
    /// Blood level at which the compensation reaches <see cref="BloodTachycardia"/>.
    /// </summary>
    [DataField]
    public float BloodCompensationFull = 0.55f;

    /// <summary>
    /// Blood level below which the heart starts compensating with tachycardia.
    /// </summary>
    [DataField]
    public float BloodCompensationStart = 0.85f;

    /// <summary>
    /// Blood level at which the heart can no longer sustain a rhythm.
    /// </summary>
    [DataField]
    public float BloodDecompensationEnd = 0.3f;

    /// <summary>
    /// Blood level below which the heart starts to fail (hemorrhagic shock).
    /// </summary>
    [DataField]
    public float BloodDecompensationStart = 0.5f;

    /// <summary>
    /// Maximum extra BPM caused by hypovolemia.
    /// </summary>
    [DataField]
    public float BloodTachycardia = 90f;

    #endregion

    #region Damage

    /// <summary>
    /// Maps wound severities of the heart to its damage factor (0 - intact, 1 - destroyed).
    /// </summary>
    [DataField]
    public Dictionary<WoundSeverity, float> HealthFactorThresholds = new()
    {
        { WoundSeverity.Healthy, 0f },
        { WoundSeverity.Minor, 0.35f },
        { WoundSeverity.Moderate, 0.6f },
        { WoundSeverity.Severe, 0.85f },
        { WoundSeverity.Critical, 1f }
    };

    /// <summary>
    /// Fraction of the cardiac output lost at full damage. Lowers the metabolic rate provided by the heart.
    /// </summary>
    [DataField]
    public float DamageOutputPenalty = 0.5f;

    /// <summary>
    /// Extra BPM at full damage: a weakened heart pumps less per beat, so it beats more often.
    /// </summary>
    [DataField]
    public float DamageTachycardia = 30f;

    #endregion

    #region Metabolism

    /// <summary>
    /// How strongly a change in heart rate is reflected in the metabolic rate.
    /// 1 - metabolic rate is proportional to the rate relative to resting, 0 - heart rate doesn't affect it.
    /// </summary>
    [DataField]
    public float MetabolicResponse = 0.75f;

    #endregion

    #region Pain

    /// <summary>
    /// Pain scale for the pain response: at this much pain ~63% of <see cref="PainTachycardia"/> is applied.
    /// </summary>
    [DataField]
    public float PainScale = 120f;

    /// <summary>
    /// Maximum extra BPM caused by pain.
    /// </summary>
    [DataField]
    public float PainTachycardia = 50f;

    #endregion

    #region Rate

    /// <summary>
    /// If both the target and current rate drop below this value, the heart goes into cardiac arrest.
    /// </summary>
    [DataField]
    public float ArrestRate = 20f;

    /// <summary>
    /// Time constant (seconds) of the rate falling towards its target. Calming down takes longer.
    /// </summary>
    [DataField]
    public float FallTime = 10f;

    /// <summary>
    /// The maximum heart rate (BPM). The sympathetic drive saturates towards this value.
    /// </summary>
    [DataField]
    public float MaxRate = 200f;

    /// <summary>
    /// An arrested heart restarts on its own only if the conditions allow a target rate of at least this value.
    /// </summary>
    [DataField]
    public float RestartRate = 45f;

    /// <summary>
    /// The average resting heart rate (BPM) for this kind of heart.
    /// </summary>
    [DataField]
    public float RestingRate = 70f;

    /// <summary>
    /// Maximum random deviation of an individual heart's resting rate from <see cref="RestingRate"/>.
    /// Rolled once when the heart is created.
    /// </summary>
    [DataField]
    public float RestingRateVariation = 8f;

    /// <summary>
    /// Time constant (seconds) of the rate rising towards its target. Sympathetic response is fast.
    /// </summary>
    [DataField]
    public float RiseTime = 3f;

    /// <summary>
    /// How often the heart rate is recalculated.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    #endregion

    #region Saturation

    /// <summary>
    /// Oxygen saturation at which the compensation reaches <see cref="SaturationTachycardia"/>.
    /// </summary>
    [DataField]
    public float SaturationCompensationFull = 0.6f;

    /// <summary>
    /// Oxygen saturation below which the heart starts compensating with tachycardia.
    /// </summary>
    [DataField]
    public float SaturationCompensationStart = 0.9f;

    /// <summary>
    /// Oxygen saturation at which the heart can no longer sustain a rhythm.
    /// </summary>
    [DataField]
    public float SaturationDecompensationEnd = 0.1f;

    /// <summary>
    /// Oxygen saturation below which hypoxic bradycardia begins.
    /// </summary>
    [DataField]
    public float SaturationDecompensationStart = 0.45f;

    /// <summary>
    /// Maximum extra BPM caused by hypoxia.
    /// </summary>
    [DataField]
    public float SaturationTachycardia = 45f;

    #endregion

    #region State

    /// <summary>
    /// Indicates whether the heart is currently beating.
    /// </summary>
    [ViewVariables]
    public bool Beating = true;

    /// <summary>
    /// The body entity containing this heart, if any.
    /// </summary>
    [ViewVariables]
    public EntityUid? Body;

    /// <summary>
    /// The smoothed heart rate without beat-to-beat variability.
    /// </summary>
    [ViewVariables]
    public float BaseRate;

    /// <summary>
    /// Last known blood level of the <see cref="Body"/> (0..1).
    /// </summary>
    [ViewVariables]
    public float BloodLevel = 1f;

    /// <summary>
    /// The current sympathetic drive (extra BPM before saturation).
    /// </summary>
    [ViewVariables]
    public float Drive;

    /// <summary>
    /// The heart's damage factor (0..1).
    /// </summary>
    [ViewVariables]
    public float HealthFactor;

    /// <summary>
    /// Last known pain of the <see cref="Body"/>.
    /// </summary>
    [ViewVariables]
    public float Pain;

    /// <summary>
    /// The individual resting rate of this heart.
    /// </summary>
    [ViewVariables]
    public float PersonalRestingRate;

    /// <summary>
    /// The current heart rate in beats per minute (BPM), including variability.
    /// </summary>
    [ViewVariables]
    public float Rate;

    /// <summary>
    /// Last known oxygen saturation level of the <see cref="Body"/> (0..1).
    /// </summary>
    [ViewVariables]
    public float SaturationLevel = 1f;

    /// <summary>
    /// Phase offset of the sinus arrhythmia so hearts don't oscillate in sync.
    /// </summary>
    [ViewVariables]
    public float SinusPhase;

    /// <summary>
    /// The rate the heart is currently moving towards.
    /// </summary>
    [ViewVariables]
    public float TargetRate;

    /// <summary>
    /// The time of the last rate update.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan LastUpdate = TimeSpan.Zero;

    /// <summary>
    /// The scheduled time for the next rate update.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// The heart's current wound severity.
    /// </summary>
    [ViewVariables]
    public WoundSeverity Severity = WoundSeverity.Healthy;

    #endregion

    #region Variability

    /// <summary>
    /// Extra random deviation (BPM) at full damage - arrhythmia of a damaged heart.
    /// </summary>
    [DataField]
    public float ArrhythmiaDeviation = 15f;

    /// <summary>
    /// Random beat-to-beat deviation (BPM) of a healthy heart.
    /// </summary>
    [DataField]
    public float RateDeviation = 2f;

    /// <summary>
    /// Amplitude (BPM) of the respiratory sinus arrhythmia - a smooth, natural oscillation of the rate.
    /// </summary>
    [DataField]
    public float SinusAmplitude = 2.5f;

    /// <summary>
    /// Period (seconds) of the respiratory sinus arrhythmia.
    /// </summary>
    [DataField]
    public float SinusPeriod = 5f;

    #endregion
}
