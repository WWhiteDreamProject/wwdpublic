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

    #region Keybind

    public static readonly CVarDef<bool> AutoGetUp =
        CVarDef.Create("white.auto_get_up", true, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED);

    public static readonly CVarDef<bool> HoldLookUp =
        CVarDef.Create("white.hold_look_up", false, CVar.CLIENT | CVar.ARCHIVE);

    #endregion

    #region Locale

    public static readonly CVarDef<string>
        ServerCulture = CVarDef.Create("white.culture", "ru-RU", CVar.REPLICATED | CVar.SERVER);

    #endregion

    #region GhostRespawn
    public static readonly CVarDef<float> GhostRespawnTime =
        CVarDef.Create("ghost.respawn_time", 15f, CVar.SERVERONLY);

    public static readonly CVarDef<int> GhostRespawnMaxPlayers =
        CVarDef.Create("ghost.respawn_max_players", 40, CVar.SERVERONLY);
    #endregion
}
