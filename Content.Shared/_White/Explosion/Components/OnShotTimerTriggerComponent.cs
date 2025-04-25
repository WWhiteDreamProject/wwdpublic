using Robust.Shared.GameStates;

namespace Content.Shared._White.Explosion.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class OnShotTimerTriggerComponent : Component
{
    /// <summary>
    ///     The timer will be reduced for that many seconds when shot.
    /// </summary>
    [DataField] public float DelayReduction;
}
