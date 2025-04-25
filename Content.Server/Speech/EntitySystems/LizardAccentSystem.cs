using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class LizardAccentSystem : EntitySystem
{
    // English letters
    private static readonly Regex RegexLowerS = new("s+");
    private static readonly Regex RegexUpperS = new("S+");
    private static readonly Regex RegexInternalX = new(@"(\w)x");
    private static readonly Regex RegexLowerEndX = new(@"\bx([\-|r|R]|\b)");
    private static readonly Regex RegexUpperEndX = new(@"\bX([\-|r|R]|\b)");
    
    // WD EDIT START
    // Russian letters
    private static readonly Regex RegexLowerZ = new("з");
    private static readonly Regex RegexUpperZ = new("З");
    private static readonly Regex RegexLowerS_Ru = new("с");
    private static readonly Regex RegexUpperS_Ru = new("С");
    private static readonly Regex RegexLowerSh = new("ш");
    private static readonly Regex RegexUpperSh = new("Ш");
    private static readonly Regex RegexLowerSch = new("щ");
    private static readonly Regex RegexUpperSch = new("Щ");
    private static readonly Regex RegexLowerTs = new("ц");
    private static readonly Regex RegexUpperTs = new("Ц");
    // WD EDIT END

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LizardAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, LizardAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // English replacements
        // hissss
        message = RegexLowerS.Replace(message, "sss");
        // hiSSS
        message = RegexUpperS.Replace(message, "SSS");
        // ekssit
        message = RegexInternalX.Replace(message, "$1kss");
        // ecks
        message = RegexLowerEndX.Replace(message, "ecks$1");
        // eckS
        message = RegexUpperEndX.Replace(message, "ECKS$1");

        // WD EDIT START
        // Russian replacements
        // З -> С
        message = RegexLowerZ.Replace(message, "с");
        message = RegexUpperZ.Replace(message, "С");
        // Растягиваем С
        message = RegexLowerS_Ru.Replace(message, "ссс");
        message = RegexUpperS_Ru.Replace(message, "ССС");
        // Растягиваем Ш
        message = RegexLowerSh.Replace(message, "шшш");
        message = RegexUpperSh.Replace(message, "ШШШ");
        // Растягиваем Щ
        message = RegexLowerSch.Replace(message, "щщщ");
        message = RegexUpperSch.Replace(message, "ЩЩЩ");
        // Ц -> СС
        message = RegexLowerTs.Replace(message, "сс");
        message = RegexUpperTs.Replace(message, "СС");
        // WD EDIT END

        args.Message = message;
    }
}
