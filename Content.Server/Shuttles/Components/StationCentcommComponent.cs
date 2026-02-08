using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// Spawns Central Command (emergency destination) for a station.
/// </summary>
[RegisterComponent]
public sealed partial class StationCentcommComponent : Component
{
    /// <summary>
    /// Crude shuttle offset spawning.
    /// </summary>
    [DataField]
    public float ShuttleIndex;

    [DataField]
    public List<ResPath> Maps = new()
    {
        ///new("/Maps/CentralCommand/main.yml"), WWDP edit, and now we have GREAT HUB
        /// new("/Maps/CentralCommand/harmony.yml") WWDP edit, no more CentCom rotation
        new("/Maps/_White/CentralCommand/hub.yml"), // WD EDIT
    };

    /// <summary>
    /// Centcomm entity that was loaded.
    /// </summary>
    [DataField]
    public EntityUid? Entity;

    [DataField]
    public EntityUid? MapEntity;
}
