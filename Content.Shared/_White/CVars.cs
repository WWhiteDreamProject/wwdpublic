using Robust.Shared.Configuration;

namespace Content.Shared._White;

[CVarDefs]
public sealed class WhiteCVars
{
    #region Aspects

    public static readonly CVarDef<bool> IsAspectsEnabled =
        CVarDef.Create("aspects.enabled", false, CVar.SERVERONLY);

    public static readonly CVarDef<double> AspectChance =
        CVarDef.Create("aspects.chance", 0.1d, CVar.SERVERONLY);

    #endregion
}
