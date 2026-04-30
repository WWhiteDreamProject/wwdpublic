using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class MothAccentSystem : EntitySystem
{
    private static readonly Regex RegexLowerBuzz = new Regex("z{1,3}");
    private static readonly Regex RegexUpperBuzz = new Regex("Z{1,3}");
    // WWDP EDIT START
    // RUSSIAN LOCALIZATION
    private static readonly Regex RegexLowerZ_Ru = new Regex("з{1,3}");
    private static readonly Regex RegexUpperZ_Ru = new Regex("З{1,3}");
    private static readonly Regex RegexLowerZh_Ru = new Regex("ж{1,3}");
    private static readonly Regex RegexUpperZh_Ru = new Regex("Ж{1,3}");
    // WWDP EDIT END

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MothAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, MothAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // buzzz
        message = RegexLowerBuzz.Replace(message, "zzz");
        // buZZZ
        message = RegexUpperBuzz.Replace(message, "ZZZ");
        // WWDP EDIT START
        message = RegexLowerZ_Ru.Replace(message, "ззз");
        message = RegexUpperZ_Ru.Replace(message, "ЗЗЗ");
        message = RegexLowerZh_Ru.Replace(message, "жжж");
        message = RegexUpperZh_Ru.Replace(message, "ЖЖЖ");
        // WWDP EDIT END

        args.Message = message;
    }
}
