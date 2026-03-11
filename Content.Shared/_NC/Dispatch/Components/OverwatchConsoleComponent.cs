using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;

namespace Content.Shared._NC.Dispatch.Components
{
    /// <summary>
    ///     Marker component for the Overwatch dispatch console ("Око Сити").
    ///     Stores active alerts and cooldown state per camera.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class OverwatchConsoleComponent : Component
    {
        /// <summary>
        ///     Active alerts currently displayed on this console.
        ///     Keyed by the alert ID for quick UI updates.
        /// </summary>
        public Dictionary<int, OverwatchAlertData> ActiveAlerts = new();

        /// <summary>
        ///     Simple mapping from camera entity to last time an alert was raised.
        ///     Used to implement the per-camera cooldown.
        /// </summary>
        public Dictionary<EntityUid, float> LastAlertTime = new();

        /// <summary>
        ///     Incremented each time we create a new alert so that IDs stay unique.
        /// </summary>
        public int NextAlertId = 1;
    }
}
