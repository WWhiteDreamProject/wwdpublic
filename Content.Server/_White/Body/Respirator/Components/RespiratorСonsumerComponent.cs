using Content.Shared.FixedPoint;

namespace Content.Server._White.Body.Respirator.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class RespiratorСonsumerComponent : Component
{
    /// <summary>
    /// How much of the body's oxidant saturation does a given organ/body part consume?
    /// </summary>
    [DataField]
    public float SaturationConsumption = 0.01f;

    /// <summary>
    /// Damage to the organ with each update at low oxygen levels in the blood.
    /// </summary>
    [DataField]
    public FixedPoint2 Damage = FixedPoint2.New(0.1);

    /// <summary>
    /// What percentage of available oxygen in the blood is needed to avoid damage caused by blood loss?
    /// </summary>
    [DataField]
    public float DamageThreshold = 0.5f;

    /// <summary>
    /// The interval between updates.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Current blood level in the body.
    /// </summary>
    [ViewVariables]
    public float BloodLevel = 1f;

    /// <summary>
    /// Body metabolic rate.
    /// </summary>
    [ViewVariables]
    public float MetabolicRate = 1f;

    /// <summary>
    /// Current blood saturation level in the body.
    /// </summary>
    [ViewVariables]
    public float SaturationLevel = 1f;

    /// <summary>
    /// The next time update.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}
