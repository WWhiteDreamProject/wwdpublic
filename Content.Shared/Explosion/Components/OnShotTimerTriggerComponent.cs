using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Explosion.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class OnShotTimerTriggerComponent : Component // WWDP
    {
        /// <summary>
        ///     The timer will be reduced for that many seconds when shot.
        /// </summary>
        [DataField] public float DelayReduction;
    }
}
