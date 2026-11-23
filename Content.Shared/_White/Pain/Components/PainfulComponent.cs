using Content.Shared._White.Pain.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Pain.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedPainfulSystem))]
public sealed partial class PainfulComponent : Component
{
    /// <summary>
    /// The raw, accumulated amount of pain. This is the base value before modifiers.
    /// </summary>
    [DataField]
    public FixedPoint2 Pain = FixedPoint2.Zero;

    /// <summary>
    /// A multiplier applied to the current raw pain level to determine the effective pain.
    /// </summary>
    [DataField]
    public float PainMultiplier = 1f;

    /// <summary>
    /// A multiplier applied to the base update interval, affecting how frequently fading pain is recalculated.
    /// </summary>
    [DataField]
    public float UpdateIntervalMultiplier = 1f;

    /// <summary>
    /// The base interval at which pain is recalculated while it is fading.
    /// When nothing fades, pain is only recalculated when its sources change.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The currently effective pain value, calculated by applying the PainMultiplier to the raw Pain.
    /// </summary>
    [ViewVariables]
    public FixedPoint2 CurrentPain => Pain * PainMultiplier;

    /// <summary>
    /// The currently effective update interval, calculated by applying the UpdateIntervalMultiplier to the base UpdateInterval.
    /// </summary>
    [ViewVariables]
    public TimeSpan CurrentUpdateInterval => UpdateInterval * UpdateIntervalMultiplier;

    /// <summary>
    /// When the pain should be recalculated next because some of it is fading. Null if nothing fades.
    /// Not networked: every side schedules it on its own.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan? NextUpdate;
}

[Serializable, NetSerializable]
public sealed class PainfulComponentState(PainfulComponent component) : ComponentState
{
    public readonly FixedPoint2 Pain = component.Pain;
    public readonly float PainMultiplier = component.PainMultiplier;
    public readonly float UpdateIntervalMultiplier = component.UpdateIntervalMultiplier;
}
