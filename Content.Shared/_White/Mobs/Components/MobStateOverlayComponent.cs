using Content.Shared._White.Mobs.Systems;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;


namespace Content.Shared._White.Mobs.Components;

/// <summary>
/// Manages visual overlay effects for different mob state.
/// Each state has configurable visual parameters for vignette-style effects.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedMobStateOverlaySystem))]
public sealed partial class MobStateOverlayComponent : Component
{
    /// <summary>
    /// Visual overlay configurations for each mob state.
    /// </summary>
    [DataField]
    public Dictionary<MobState, MobStateOverlayData> Data = new()
    {
        {
            MobState.Alive, new()
            {
                Color = Color.FromHex("#400303"),
                InnerMax = 0.6f,
                InnerMin = 0.2f,
                InnerPulse = 0.02f,
                OuterAlpha = 0.8f,
                OuterMax = 2.0f,
                OuterMin = 0.8f,
                OuterPulse = 0.2f,
                Rate = 3.0f,
            }
        },
        {
            MobState.Critical, new()
            {
                Color = Color.Black,
                InnerMax = 0.02f,
                InnerMin = 0.02f,
                OuterAlpha = 0.98f,
                OuterMax = 0.6f,
                OuterMin = 0.06f,
            }
        },
        {
            MobState.Dead, new()
            {
                Color = Color.Black,
                InnerMax = 0.02f,
                InnerMin = 0.02f,
                OuterAlpha = 0.98f,
                OuterMax = 0.6f,
                OuterMin = 0.06f,
            }
        },
    };

    /// <summary>
    /// Interpolation speed for smooth transitions between overlay intensity levels.
    /// Controls how quickly the overlay fades in/out when the target level changes.
    /// </summary>
    [DataField]
    public float Speed = 5f;

    /// <summary>
    /// The current intensity level of the overlay, interpolated towards the target level.
    /// </summary>
    [ViewVariables]
    public float Level;

    /// <summary>
    /// The current mob state, determining which overlay configuration is active.
    /// </summary>
    [ViewVariables]
    public MobState State = MobState.Alive;
}

[Serializable, NetSerializable]
public sealed class MobStateOverlayState(MobStateOverlayComponent component) : ComponentState
{
    public readonly MobState State = component.State;
}
