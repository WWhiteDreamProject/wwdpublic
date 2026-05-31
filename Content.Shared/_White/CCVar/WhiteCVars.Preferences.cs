using Robust.Shared.Configuration;

namespace Content.Shared._White.CCVar;

public sealed partial class WhiteCVars
{
    /// <summary>
    /// Sets the maximum length for custom content.
    /// </summary>
    public static readonly CVarDef<int> MaxCustomContentLength =
        CVarDef.Create("preferences.max_custom_content_length", 524288, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Sets the maximum length for flavor.
    /// </summary>
    public static readonly CVarDef<int> MaxFlavorLength =
        CVarDef.Create("preferences.max_flavor_length", 1024, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Sets the maximum length for name.
    /// </summary>
    public static readonly CVarDef<int> MaxNameLength =
        CVarDef.Create("preferences.max_name_length", 64, CVar.SERVER | CVar.REPLICATED);
}
