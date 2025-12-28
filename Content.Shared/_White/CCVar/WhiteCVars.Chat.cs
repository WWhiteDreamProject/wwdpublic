using Robust.Shared.Configuration;

namespace Content.Shared._White.CCVar;

public sealed partial class WhiteCVars
{
    public static readonly CVarDef<bool> LogInChat =
        CVarDef.Create("chat.log", true, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED);

    public static readonly CVarDef<bool> ChatFancyFont =
        CVarDef.Create("chat.fancy_font", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> ColoredBubbleChat =
        CVarDef.Create("chat.colored_bubble", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> SingleBubbleCharLimit =
        CVarDef.Create("chat.bubble_character_limit", 43, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> SpeechBubbleCap =
        CVarDef.Create("chat.bubble_max_count", 1, CVar.SERVER | CVar.REPLICATED);
}
