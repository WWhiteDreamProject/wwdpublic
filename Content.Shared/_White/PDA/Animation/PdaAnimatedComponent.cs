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
    public float OpeningDuration = 1.65f; // ( 34 - 1 frames ) * 0.05s

    /// <summary>
    /// Duration of closing animation in seconds
    /// </summary>
    [DataField]
    public float ClosingDuration = 0.7f; // ( 15 - 1 frames ) * 0.05s

    /// <summary>
    /// The user who is currently opening/closing the PDA
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AnimatingUser;

    [DataField, AutoNetworkedField]
    public bool ClosingAnimationStarted { get; set; } = false;
}

[Serializable, NetSerializable]
public enum PdaAnimationState : byte
{
    Closed,  // pda
    Opening, // screen_turning-on
    Open,    // pda_on
    Closing  // screen_shutdown
}
