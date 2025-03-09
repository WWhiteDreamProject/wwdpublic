using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class NukieMouseAccentSystem : EntitySystem

{
    [Dependency] private readonly IRobustRandom _random = default!;

    //RU
    private static readonly Regex RegexLowerO = new("о");
    private static readonly Regex RegexUpperO = new("О");
    private static readonly Regex RegexLowerEE = new("ы|э|е|ё|и");
    private static readonly Regex RegexUpperEE = new("Ы|Э|Е|Ё|И");
    private static readonly Regex RegexLowerSh = new("ш");
    private static readonly Regex RegexUpperSh = new("Ш");
    private static readonly Regex RegexLowerF = new("ф|г");
    private static readonly Regex RegexUpperF = new("Ф|Г");
    private static readonly Regex RegexLowerZh = new("ж");
    private static readonly Regex RegexUpperZh = new("Ж");
    private static readonly Regex RegexLowerZ = new("з+");
    private static readonly Regex RegexUpperZ = new("З+");
    private static readonly Regex RegexLowerP = new("т|б|п+");
    private static readonly Regex RegexUpperP = new("Т|Б|П+");
    private static readonly Regex RegexLowerCh = new("ц");
    private static readonly Regex RegexUpperCh = new("Ц");



    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NukieMouseAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, NukieMouseAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        //RU
        message = RegexLowerO.Replace(message, _random.Prob(0.95f) ? "у" : "ии");
        message = RegexUpperO.Replace(message, _random.Prob(0.95f) ? "У" : "Ии");
        message = RegexLowerEE.Replace(message, _random.Prob(0.8f) ? "ии" : "и");
        message = RegexUpperEE.Replace(message, _random.Prob(0.8f) ? "Ии" : "И");
        message = RegexLowerSh.Replace(message, "щ");
        message = RegexUpperSh.Replace(message, "Щ");
        message = RegexLowerF.Replace(message, "в");
        message = RegexUpperF.Replace(message, "В");
        message = RegexLowerZh.Replace(message, _random.Prob(0.5f) ? "ш" : "жь");
        message = RegexUpperZh.Replace(message, _random.Prob(0.5f) ? "Ш" : "Жь");
        message = RegexLowerZ.Replace(message, _random.Prob(0.5f) ? "с" : "сь");
        message = RegexUpperZ.Replace(message, _random.Prob(0.5f) ? "С" : "Сь");
        message = RegexLowerP.Replace(message, _random.Prob(0.8f) ? "пи" : "пик");
        message = RegexUpperP.Replace(message, _random.Prob(0.8f) ? "Пи" : "Пик");
        message = RegexLowerCh.Replace(message, "ч");
        message = RegexUpperCh.Replace(message, "Ч");

        args.Message = message;
    }
}
