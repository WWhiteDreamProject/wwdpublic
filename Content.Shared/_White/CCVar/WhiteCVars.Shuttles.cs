using Robust.Shared.Configuration;

namespace Content.Shared._White.CCVar;

public sealed partial class WhiteCVars
{
    /// <summary>
    /// Can emergency shuttle's early launch authorizations be recalled.
    /// </summary>
    public static readonly CVarDef<bool> EmergencyAuthRecallAllowed =
        CVarDef.Create("shuttle.emergency_auth_recall_allowed", false, CVar.SERVERONLY);

    /// <summary>
    /// How far can the shuttle use the FTL.
    /// </summary>
    public static readonly CVarDef<float> FTLRange =
        CVarDef.Create("shuttle.range", 512f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// How close can the shuttles use the FTL.
    /// </summary>
    public static readonly CVarDef<float> FTLBufferRange =
        CVarDef.Create("shuttle.buffer_range", 8f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);
}
