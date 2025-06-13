using Robust.Shared.Configuration;

namespace Content.Shared._White.CCVar;

public sealed partial class WhiteCVars
{
    /// <summary>
    /// Shove range multiplier.
    /// </summary>
    public static readonly CVarDef<float> ShoveRange =
        CVarDef.Create("shoving.range", 1f, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Shove speed multiplier, does not affect range.
    /// </summary>
    public static readonly CVarDef<float> ShoveSpeed =
        CVarDef.Create("shoving.speed", 3f, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// How much should the mass difference affect shove range & speed.
    /// </summary>
    public static readonly CVarDef<float> ShoveMassFactor =
        CVarDef.Create("shoving.mass_factor", 5f, CVar.SERVER | CVar.ARCHIVE);
}
