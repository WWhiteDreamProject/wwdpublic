using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._White
{
    [CVarDefs]
    public sealed class WhiteCCVars : CVars
    {

        /*
         * IDK
         */

        public static readonly CVarDef<string>
            ServerCulture = CVarDef.Create("white.culture", "ru-RU", CVar.REPLICATED | CVar.SERVER);

    }
}
