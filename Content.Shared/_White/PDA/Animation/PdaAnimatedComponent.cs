using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.PDA.Animation;

/// <summary>
/// Manages PDA opening/closing animation states and timing.
/// Handles animation transitions between closed, opening, open, and closing states,
/// with configurable durations and user tracking to prevent animation conflicts.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PdaAnimatedComponent : Component
{
    [DataField, AutoNetworkedField]
    public PdaAnimationState AnimationState = PdaAnimationState.Closed;

    /// <summary>
    /// Duration of opening animation in seconds
    /// </summary>
    [DataField]
    public float OpeningDuration = 1.02f; // 34 frames * 0.03s

    /// <summary>
    /// Duration of closing animation in seconds
    /// </summary>
    [DataField]
    public float ClosingDuration = 0.45f; // 15 frames * 0.03s

    /// <summary>
    /// The user who is currently opening/closing the PDA
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AnimatingUser;

    /// <summary>
    /// Accumulator for tracking animation time
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AnimationTimeAccumulator = 0f;
}

[Serializable, NetSerializable]
public enum PdaAnimationState : byte
{
    Closed,  // pda
    Opening, // screen_turning-on
    Open,    // pda_on
    Closing  // screen_shutdown
}
