using System;

namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Self-recharging battery.
    /// </summary>
    [RegisterComponent]
    public sealed partial class BatterySelfRechargerComponent : Component
    {
        /// <summary>
        /// Does the entity auto recharge?
        /// </summary>
        [DataField] public bool AutoRecharge;

        /// <summary>
        /// At what rate does the entity automatically recharge?
        /// </summary>
        [DataField] public float AutoRechargeRate;

        /// <summary>
        /// Should this entity stop automatically recharging if a charge is used?
        /// </summary>
        [DataField] public bool AutoRechargePause = false;

        /// <summary>
        /// How long should the entity stop automatically recharging if a charge is used?
        /// </summary>
        [DataField] public float AutoRechargePauseTime = 0f;

        /// <summary>
        /// Do not auto recharge if this timestamp has yet to happen, set for the auto recharge pause system.
        /// </summary>
        [DataField] public TimeSpan NextAutoRecharge = TimeSpan.FromSeconds(0f);

        // WWDP edit start
        /// <summary>
        /// Interval in seconds for cyclic recharge. 0 = disabled (normal operation)
        /// </summary>
        [DataField] public float CyclicRechargeInterval = 0f;

        /// <summary>
        /// Amount of charge to add during each cyclic recharge
        /// </summary>
        [DataField] public float CyclicRechargeAmount = 0f;

        /// <summary>
        /// Next time to perform cyclic recharge
        /// </summary>
        [DataField] public TimeSpan NextCyclicRecharge = TimeSpan.FromSeconds(0f);
        // WWDP edit end
    }
}
