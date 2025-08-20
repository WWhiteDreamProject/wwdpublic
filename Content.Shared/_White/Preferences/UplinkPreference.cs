using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// Type of uplink device used by the traitor
    /// </summary>
    [Serializable, NetSerializable]
    public enum UplinkPreference : byte
    {
        /// <summary>
        /// Standard PDA uplink (20 TC)
        /// </summary>
        PDA = 0,

        /// <summary>
        /// Implanted uplink (18 TC)
        /// </summary>
        Implant = 1,

        /// <summary>
        /// Radio uplink (21 TC)
        /// </summary>
        Radio = 2,

        /// <summary>
        /// Direct telecrystals (25 TC)
        /// </summary>
        Telecrystals = 3
    }
}