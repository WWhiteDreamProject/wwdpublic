using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Dispatch.Components
{
    /// <summary>
    ///     Component applied to surveillance cameras that lets them function as
    ///     acoustic sensors for the Overwatch dispatch network.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class AcousticSensorComponent : Component
    {
        /// <summary>
        ///     Maximum range in tiles at which gunfire can be detected.
        ///     This value is reduced when the shooter uses a suppressed weapon.
        /// </summary>
        [DataField("gunRange")] public float GunRange = 10f;

        /// <summary>
        ///     Reduced range when the weapon is "silenced".
        /// </summary>
        [DataField("suppressedRange")] public float SuppressedRange = 3f;

        /// <summary>
        ///     Detection radius for explosions. Does not perform line-of-sight checks.
        /// </summary>
        [DataField("explosionRange")] public float ExplosionRange = 20f;

        /// <summary>
        ///     Whether the sensor is currently enabled (camera powered & active).
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled = true;
    }
}
