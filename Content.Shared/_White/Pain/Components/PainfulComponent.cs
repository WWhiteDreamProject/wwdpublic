using Content.Shared._White.Pain.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Pain.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SharedPainfulSystem))]
public sealed partial class PainfulComponent : Component
{
    /// <summary>
    /// The raw, accumulated amount of pain. This is the base value before modifiers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Pain = FixedPoint2.Zero;

    /// <summary>
    /// The maximum rate at which pain can decrease per second, independent of multiplier.
    /// </summary>
    [DataField]
    public float MaxPainDecreasePerSecond = 12f;

    /// <summary>
    /// The maximum rate at which pain can increase per second, independent of multiplier.
    /// </summary>
    [DataField]
    public float MaxPainIncreasePerSecond = 36f;

    /// <summary>
    /// A multiplier applied to the current raw pain level to determine the effective pain.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PainMultiplier = 1f;

    /// <summary>
    /// A multiplier applied to the base update interval, affecting how frequently pain is recalculated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float UpdateIntervalMultiplier = 1f;

    /// <summary>
    /// The base interval at which pain recalculations are performed.
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
    /// The timestamp of the last time the pain was updated.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public TimeSpan LastUpdate = TimeSpan.Zero;
}


[Serializable, NetSerializable]
public sealed class PainfulComponentState(PainfulComponent component) : ComponentState
{
    public readonly FixedPoint2 Pain = component.Pain;
    public readonly float PainMultiplier = component.PainMultiplier;
    public readonly float UpdateIntervalMultiplier = component.UpdateIntervalMultiplier;
    public readonly TimeSpan LastUpdate = component.LastUpdate;
}
