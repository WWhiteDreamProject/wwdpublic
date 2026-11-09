using Content.Shared._White.Pain.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Pain.Components;

[RegisterComponent, NetworkedComponent]
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
    /// A multiplier applied to the base update interval, affecting how frequently pain is recalculated.
    /// </summary>
    [DataField]
    public float UpdateIntervalMultiplier = 1f;

    /// <summary>
    /// The base interval at which pain recalculations are performed.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Determines whether the painful entity is dead. If true, the pain is not processed.
    /// </summary>
    [ViewVariables]
    public bool Dead;

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
    /// The timestamp of the last time the pain was updated.
    /// </summary>
    [ViewVariables]
    public TimeSpan LastUpdate = TimeSpan.Zero;
}

[Serializable, NetSerializable]
public sealed class PainfulComponentState(PainfulComponent component) : ComponentState
{
    public readonly bool Dead = component.Dead;
    public readonly FixedPoint2 Pain = component.Pain;
    public readonly float PainMultiplier = component.PainMultiplier;
    public readonly float UpdateIntervalMultiplier = component.UpdateIntervalMultiplier;
    public readonly TimeSpan LastUpdate = component.LastUpdate;
}
