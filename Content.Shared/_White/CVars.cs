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

    #region Locale

    public static readonly CVarDef<string>
        ServerCulture = CVarDef.Create("white.culture", "ru-RU", CVar.REPLICATED | CVar.SERVER);

    #endregion

    #region OptionsMisc

    public static readonly CVarDef<bool> LogInChat =
        CVarDef.Create("white.log_in_chat", true, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED);

    #endregion

    #region TTS

    /// <summary>
    /// if the TTS system enabled or not.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static readonly CVarDef<bool> TTSEnabled = CVarDef.Create("tts.enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// URL of the TTS server API.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static readonly CVarDef<string> TTSApiUrl = CVarDef.Create("tts.api_url", "", CVar.SERVERONLY);

    /// <summary>
    /// Auth token of the TTS server API.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static readonly CVarDef<string> TTSApiToken =
        CVarDef.Create("tts.api_token", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// The volume of TTS playback.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static readonly CVarDef<float> TTSVolume = CVarDef.Create("tts.volume", 0f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// TTS Cache.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static readonly CVarDef<int> TTSMaxCache =
        CVarDef.Create("tts.max_cash_size", 200, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Amount of seconds before timeout for API
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static readonly CVarDef<int> TTSApiTimeout =
        CVarDef.Create("tts.api_timeout", 5, CVar.SERVERONLY | CVar.ARCHIVE);

    #endregion
}
