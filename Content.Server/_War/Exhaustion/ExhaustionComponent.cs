using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._War.Exhaustion;

/// <summary>
/// Component that tracks exhaustion buildup from prolonged starvation.
/// Used in conjunction with HungerComponent to handle starvation effects.
/// </summary>
[RegisterComponent]
public sealed partial class ExhaustionComponent : Component
{
    /// <summary>
    /// Current exhaustion value. Increases when starving and decreases when eating.
    /// </summary>
    [DataField("currentExhaustion")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 CurrentExhaustion;

    /// <summary>
    /// Maximum exhaustion value. When reached, stops accumulation.
    /// </summary>
    [DataField("maxExhaustion")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaxExhaustion = 100;

    /// <summary>
    /// Rate at which exhaustion accumulates when starving.
    /// Set to reach maximum exhaustion in 15-20 minutes.
    /// </summary>
    [DataField("accumulationRate")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 AccumulationRate = 0.1f;

    /// <summary>
    /// Rate at which exhaustion heals when eating.
    /// Set to heal completely in about 50 seconds.
    /// </summary>
    [DataField("healRate")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 HealRate = 2.0f;

    /// <summary>
    /// Next time to update exhaustion values.
    /// </summary>
    [DataField("nextUpdateTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// How often the exhaustion values update.
    /// </summary>
    [DataField("updateRate")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);
}
