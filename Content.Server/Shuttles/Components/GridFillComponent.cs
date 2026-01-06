using Content.Server.Shuttles.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// If added to an airlock will try to autofill a grid onto it on MapInit
/// </summary>
[RegisterComponent, Access(typeof(ShuttleSystem))]
public sealed partial class GridFillComponent : Component
{
    [DataField]
    public ResPath Path = new("/Maps/Shuttles/escape_pod_small.yml");

    // WWDP edit start
    [DataField]
    public bool IgnoreCVars = false;
    // WWDP edit end

    /// <summary>
    /// Components to be added to any spawned grids.
    /// </summary>
    [DataField]
    public ComponentRegistry AddComponents = new();
}
