using Robust.Shared.Configuration;

namespace Content.Shared._White.CCVar;

public sealed partial class WhiteCVars
{
    /// <summary>
    /// Default volume setting of TTS sound
    /// </summary>
    public static readonly CVarDef<float> BarkVolume =
        CVarDef.Create("bark.volume", 0f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
