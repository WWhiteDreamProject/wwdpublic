using Content.Shared._White.Wounds;

namespace Content.Server._White.Bloodstream.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class HeartComponent : Component
{
    /// <summary>
    /// Maps wound severities to a corresponding health factor for the heart.
    /// </summary>
    [DataField]
    public Dictionary<WoundSeverity, float> HealthFactorThresholds = new ()
    {
        {WoundSeverity.Healthy, 0f},
        {WoundSeverity.Minor, 0.35f},
        {WoundSeverity.Moderate, 0.6f},
        {WoundSeverity.Severe, 0.85f},
        {WoundSeverity.Critical, 1f},
    };

    /// <summary>
    /// The blood level at which its influence on strain becomes most active.
    /// This is a midpoint for the logistic growth curve applied to a <see cref="BloodFactor"/>.
    /// </summary>
    [DataField]
    public float BloodInflection = 0.5f;

    /// <summary>
    /// Determines how actively the <see cref="BloodFactor"/> influences strain.
    /// Higher values lead to a steeper increase in strain for a given blood level change.
    /// </summary>
    [DataField]
    public float BloodSteepness = 10f;

    /// <summary>
    /// The maximum heart rate the heart can reach.
    /// </summary>
    [DataField]
    public float MaxRate = 220;

    /// <summary>
    /// The metabolic rate at which its influence on strain becomes most active.
    /// This is a midpoint for the logistic growth curve applied to a <see cref="MetabolicFactor"/>.
    /// </summary>
    [DataField]
    public float MetabolicInflection = 1.5f;

    /// <summary>
    /// Determines how actively the <see cref="MetabolicFactor"/> influences strain.
    /// Higher values lead to a steeper increase in strain for a given metabolic rate change.
    /// </summary>
    [DataField]
    public float MetabolicSteepness = 2f;

    /// <summary>
    /// Determines the metabolic rate per heartbeat
    /// </summary>
    [DataField]
    public float MetabolicPerBeat = 1f / 60f;

    /// <summary>
    /// The pain level at which its influence on strain becomes most active.
    /// This is a midpoint for the logistic growth curve applied to a <see cref="PainFactor"/>.
    /// </summary>
    [DataField]
    public float PainInflection = 0.03f;

    /// <summary>
    /// Determines how actively the <see cref="PainFactor"/> influences strain.
    /// Higher values lead to a steeper increase in strain for a given pain level change.
    /// </summary>
    [DataField]
    public float PainSteepness = 200f;

    /// <summary>
    /// The saturation level at which its influence on strain becomes most active.
    /// This is a midpoint for the logistic growth curve applied to a <see cref="SaturationFactor"/>.
    /// </summary>
    [DataField]
    public float SaturationInflection = 0.5f;

    /// <summary>
    /// Determines how actively the <see cref="SaturationFactor"/> influences strain.
    /// Higher values lead to a steeper increase in strain for a given saturation level change.
    /// </summary>
    [DataField]
    public float SaturationSteepness = 10f;

    /// <summary>
    /// The raw strain value at which its growth becomes more active.
    /// This acts as a midpoint for the logistic growth curve applied to <see cref="Strain"/>.
    /// </summary>
    [DataField]
    public float StrainInflection = 1.5f;

    /// <summary>
    /// Determines how actively the <see cref="Strain"/> increases with contributing factors.
    /// Higher values lead to a steeper increase in strain for the same input.
    /// </summary>
    [DataField]
    public float StrainSteepness = 3f;

    /// <summary>
    /// Determines the frequency of heart rate updates based on the calculated rate.
    /// </summary>
    [DataField]
    public float UpdateIntervalPerBeat = 180f;

    /// <summary>
    /// The maximum random deviation applied to the heart's calculated rate.
    /// Used to simulate natural variations and irregularities in heart rhythm.
    /// </summary>
    [DataField]
    public int RateDeviation = 7;

    /// <summary>
    /// Indicates whether the heart is currently enabled and actively beating.
    /// </summary>
    [ViewVariables]
    public bool Beating;

    /// <summary>
    /// The body entity containing this provider, if any.
    /// </summary>
    [ViewVariables]
    public EntityUid? Body;

    /// <summary>
    /// An indicator that reflects how much the stress level increases from the current <see cref="Body"/>'s blood level.
    /// </summary>
    [ViewVariables]
    public float BloodFactor = 1f;

    /// <summary>
    /// An indicator that reflects how much the stress level increases from the current heart's health.
    /// </summary>
    [ViewVariables]
    public float HealthFactor = 1f;

    /// <summary>
    /// An indicator that reflects how much the stress level increases from the current <see cref="Body"/>'s metabolic rate.
    /// </summary>
    [ViewVariables]
    public float MetabolicFactor = 1f;

    /// <summary>
    /// An indicator that reflects how much the stress level increases from the current <see cref="Body"/>'s pain level.
    /// </summary>
    [ViewVariables]
    public float PainFactor = 0f;

    /// <summary>
    /// The current heart rate in beats per minute (BPM).
    /// </summary>
    [ViewVariables]
    public float Rate = 0;

    /// <summary>
    /// An indicator that reflects how much the stress level increases from the current <see cref="Body"/>'s saturation level.
    /// </summary>
    [ViewVariables]
    public float SaturationFactor = 1f;

    /// <summary>
    /// The current calculated strain on the heart.
    /// This value accumulates based on various physiological factors and determines the current heart rate.
    /// Ranges from 0 to 1.
    /// </summary>
    [ViewVariables]
    public float Strain = 0f;

    /// <summary>
    /// The scheduled time for the next heart beat event.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}
