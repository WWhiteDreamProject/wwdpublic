using Robust.Shared.Configuration;

namespace Content.Shared._White.CCVar;

public sealed partial class WhiteCVars
{
    public static readonly CVarDef<string> EmotesMenuStyle =
        CVarDef.Create("interface.emotes_menu_style", "Window", CVar.CLIENT | CVar.ARCHIVE);
}
