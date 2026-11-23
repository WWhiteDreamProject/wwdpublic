using Content.Shared._White.Pain.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Pain.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(SharedPainfulSystem))]
public sealed partial class PainfulWoundComponent : Component
{
    /// <summary>
    /// The rate at which "fresh" pain decreases over time per second.
    /// This represents the initial intensity fading away.
    /// </summary>
    [DataField]
    public double FreshPainDecreasePerSecond = 0.15d;

    /// <summary>
    /// A multiplier applied to each new portion of damage to determine the acute pain it causes.
    /// Every hit adds its own fresh pain on top of what is left of the previous ones.
    /// </summary>
    [DataField(required: true)]
    public float FreshPainCoefficients;

    /// <summary>
    /// A multiplier applied to the total damage of the wound to determine the persistent pain.
    /// For example, a coefficient of 0.5 means half the damage value contributes to pain.
    /// </summary>
    [DataField(required: true)]
    public float PainCoefficients;

    /// <summary>
    /// The body entity containing this wound, if any.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Body;

    /// <summary>
    /// The temporary, acute pain at <see cref="FreshPainSince"/>.
    /// It decreases linearly by <see cref="FreshPainDecreasePerSecond"/> from that moment.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 FreshPain = FixedPoint2.Zero;

    /// <summary>
    /// The persistent amount of pain caused by the wound that does not diminish over time.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 LastingPain = FixedPoint2.Zero;

    /// <summary>
    /// When <see cref="FreshPain"/> was last changed.
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan FreshPainSince = TimeSpan.Zero;
}
