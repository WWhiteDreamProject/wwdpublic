using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class FrontalLispSystem : EntitySystem
{
    // @formatter:off
    private static readonly Regex RegexUpperTh = new(@"[T]+[Ss]+|[S]+[Cc]+(?=[IiEeYy]+)|[C]+(?=[IiEeYy]+)|[P][Ss]+|([S]+[Tt]+|[T]+)(?=[Ii]+[Oo]+[Uu]*[Nn]*)|[C]+[Hh]+(?=[Ii]*[Ee]*)|[Z]+|[S]+|[X]+(?=[Ee]+)");
    private static readonly Regex RegexLowerTh = new(@"[t]+[s]+|[s]+[c]+(?=[iey]+)|[c]+(?=[iey]+)|[p][s]+|([s]+[t]+|[t]+)(?=[i]+[o]+[u]*[n]*)|[c]+[h]+(?=[i]*[e]*)|[z]+|[s]+|[x]+(?=[e]+)");
    private static readonly Regex RegexUpperEcks = new(@"[E]+[Xx]+[Cc]*|[X]+");
    private static readonly Regex RegexLowerEcks = new(@"[e]+[x]+[c]*|[x]+");
    // WWDP EDIT START
    // RUSSIAN LOCALIZATION
    private static readonly Regex RegexLowerS_Ru = new("с");
    private static readonly Regex RegexUpperS_Ru = new("С");
    private static readonly Regex RegexLowerZ_Ru = new("з");
    private static readonly Regex RegexUpperZ_Ru = new("З");
    private static readonly Regex RegexLowerTs_Ru = new("ц");
    private static readonly Regex RegexUpperTs_Ru = new("Ц");
    // WWDP EDIT END
    // @formatter:on

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FrontalLispComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, FrontalLispComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // handles ts, sc(i|e|y), c(i|e|y), ps, st(io(u|n)), ch(i|e), z, s
        message = RegexUpperTh.Replace(message, "TH");
        message = RegexLowerTh.Replace(message, "th");
        // handles ex(c), x
        message = RegexUpperEcks.Replace(message, "EKTH");
        message = RegexLowerEcks.Replace(message, "ekth");
        // WWDP EDIT START
        // RUSSIAN LOCALIZATION
        message = RegexLowerS_Ru.Replace(message, "ш");
        message = RegexUpperS_Ru.Replace(message, "Ш");
        message = RegexLowerZ_Ru.Replace(message, "ж");
        message = RegexUpperZ_Ru.Replace(message, "Ж");
        message = RegexLowerTs_Ru.Replace(message, "ч");
        message = RegexUpperTs_Ru.Replace(message, "Ч");
        // WWDP EDIT END

        args.Message = message;
    }
}
