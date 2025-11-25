using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Medical.Pain.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PainfulComponent : Component
{
    /// <summary>
    /// The current amount of pain.
    /// </summary>
    [DataField]
    public FixedPoint2 Pain = FixedPoint2.Zero;

    /// <summary>
    /// A multiplier that is applied to the current pain level.
    /// </summary>
    [DataField]
    public float PainMultiplier = 1f;

    /// <summary>
    /// The multiplier applied to the current update intervals.
    /// </summary>
    [DataField]
    public float UpdateIntervalMultiplier = 1f;

    /// <summary>
    /// How often the pain is recalculated.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How fast the pain can increase per second
    /// </summary>
    [DataField]
    public float MaxPainIncreasePerSecond = 36f;

    /// <summary>
    /// How fast the pain can decrease per second
    /// </summary>
    [DataField]
    public float MaxPainDecreasePerSecond = 12f;

    [ViewVariables]
    public FixedPoint2 CurrentPain => Pain * PainMultiplier;

    [ViewVariables]
    public TimeSpan CurrentUpdateInterval => UpdateInterval * UpdateIntervalMultiplier;

    [ViewVariables]
    public TimeSpan LastUpdate = TimeSpan.Zero;
}

[Serializable, NetSerializable]
public sealed class PainfulComponentState(PainfulComponent component) : ComponentState
{
    public readonly FixedPoint2 Pain = component.Pain;

    public readonly float PainMultiplier = component.PainMultiplier;

    public readonly TimeSpan LastUpdate = component.LastUpdate;
}
