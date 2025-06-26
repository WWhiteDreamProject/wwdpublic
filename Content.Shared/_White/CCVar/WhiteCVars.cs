using Robust.Shared.Configuration;

namespace Content.Shared._White.CCVar;

[CVarDefs]
public sealed partial class WhiteCVars
{
    public static readonly CVarDef<bool> CoalesceIdenticalMessages =
        CVarDef.Create("white.coalesce_identical_messages", true, CVar.CLIENT | CVar.ARCHIVE | CVar.CLIENTONLY);

    public static readonly CVarDef<bool> PMMEnabled =
        CVarDef.Create("pmm.enabled", true, CVar.SERVER | CVar.ARCHIVE);

    public static readonly CVarDef<bool> PixelSnapCamera =
    	CVarDef.Create("experimental.pixel_snap_camera", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
