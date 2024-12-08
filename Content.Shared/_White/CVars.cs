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
    public static readonly CVarDef<double> GhostRespawnTime =
        CVarDef.Create("ghost.respawn_time", 15d, CVar.SERVERONLY);

    public static readonly CVarDef<int> GhostRespawnMaxPlayers =
        CVarDef.Create("ghost.respawn_max_players", 40, CVar.SERVERONLY);

    #endregion

    #region OptionsMisc

    public static readonly CVarDef<bool> LogInChat =
        CVarDef.Create("white.log_in_chat", true, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED);

    #endregion

    #region Discord
        /// <summary>
        ///     The token used to authenticate with Discord. For the Bot to function set: discord.token, discord.guild_id, and discord.prefix.
        ///     If this is empty, the bot will not connect.
        /// </summary>
        public static readonly CVarDef<string> DiscordToken =
            CVarDef.Create("discord.token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

        /// <summary>
        ///     The Discord guild ID to use for commands as well as for several other features, like the ahelp relay.
        ///     If this is empty, the bot will not connect.
        /// </summary>
        public static readonly CVarDef<string> DiscordGuildId =
            CVarDef.Create("discord.guild_id", string.Empty, CVar.SERVERONLY);

        /// <summary>
        ///     Prefix used for commands for the Discord bot.
        ///     If this is empty, the bot will not connect.
        /// </summary>
        public static readonly CVarDef<string> DiscordPrefix =
            CVarDef.Create("discord.prefix", "!", CVar.SERVERONLY);

        /// <summary>
        ///     The discord **FORUM** channel id that admin messages are sent to. If it's not a forum channel, everything will explode.
        /// </summary>
        public static readonly CVarDef<string> AdminAhelpRelayChannelId =
            CVarDef.Create("admin.ahelp_relay_channel_id", string.Empty, CVar.SERVERONLY);

        /// <summary>
        ///     If this is true, the ahelp relay shows that the response was from discord. If this is false, all messages an admin sends will be shown as if the admin was ingame.
        /// </summary>
        public static readonly CVarDef<bool> AdminAhelpRelayShowDiscord =
            CVarDef.Create("admin.ahelp_relay_show_discord", true, CVar.SERVERONLY);

        /// <summary>
        /// The discord channel id that OOC messages are sent to.
        /// </summary>
        public static readonly CVarDef<string> OocRelayChannelId =
            CVarDef.Create("ooc.relay_channel_id", "", CVar.SERVERONLY);
    #endregion
}
