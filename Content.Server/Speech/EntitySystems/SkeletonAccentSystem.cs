using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class SkeletonAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [GeneratedRegex(@"(?<!\w)[^aeiou]one", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex BoneRegex();
    // WWDP EDIT START
    // Replace «-ость» with «-КОСТЬ» (радость → радКОСТЬ, крутость → крутКОСТЬ)
    [GeneratedRegex(@"ость\b", RegexOptions.IgnoreCase)]
    private static partial Regex RuBoneRegex();
    // WWDP EDIT END

    private static readonly Dictionary<string, string> DirectReplacements = new()
    {
        { "fuck you", "I've got a BONE to pick with you" },
        { "fucked", "boned"},
        { "fuck", "RATTLE RATTLE" },
        { "fck", "RATTLE RATTLE" },
        { "shit", "RATTLE RATTLE" }, // Capitalize RATTLE RATTLE regardless of original message case.
        { "definitely", "make no bones about it" },
        { "absolutely", "make no bones about it" },
        { "afraid", "rattled"},
        { "scared", "rattled"},
        { "spooked", "rattled"},
        { "shocked", "rattled"},
        { "killed", "skeletonized"},
        { "humorous", "humerus"},
        { "to be a", "tibia"},
        { "under", "ulna"},
        { "narrow", "marrow"},
    };
    // WWDP EDIT START
    // Russian bone puns
    private static readonly Dictionary<string, string> DirectReplacements_Ru = new()
    {
        { "чёрт", "КОСТИ ГРЕМЯТ" },
        { "черт", "КОСТИ ГРЕМЯТ" },
        { "блин", "КОСТИ ГРЕМЯТ" },
        { "блять", "КОСТИ ГРЕМЯТ" },
        { "блядь", "КОСТИ ГРЕМЯТ" },
        { "сука", "КОСТИ ГРЕМЯТ" },
        { "точно", "костьми лягу" },
        { "определённо", "костьми лягу" },
        { "абсолютно", "костьми лягу" },
        { "боюсь", "костей не соберу" },
        { "страшно", "до костей пробирает" },
        { "испуган", "до костей пробрало" },
        { "шокирован", "до костей пробрало" },
        { "убит", "обглодан до костей" },
        { "смешно", "до мозга костей" },
        { "под", "до мозга костей" },
    };
    // WWDP EDIT END

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkeletonAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, SkeletonAccentComponent component)
    {
        // Order:
        // Do character manipulations first
        // Then direct word/phrase replacements
        // Then prefix/suffix

        var msg = message;

        // Character manipulations:
        // At the start of words, any non-vowel + "one" becomes "bone", e.g. tone -> bone ; lonely -> bonely; clone -> clone (remains unchanged).
        msg = BoneRegex().Replace(msg, "bone");

        // Direct word/phrase replacements:
        foreach (var (first, replace) in DirectReplacements)
        {
            msg = Regex.Replace(msg, $@"(?<!\w){first}(?!\w)", replace, RegexOptions.IgnoreCase);
        }
        // WWDP EDIT START
        msg = RuBoneRegex().Replace(msg, "КОСТЬ");

        foreach (var (first, replace) in DirectReplacements_Ru)
        {
            msg = Regex.Replace(msg, $@"(?<!\w){first}(?!\w)", replace, RegexOptions.IgnoreCase);
        }
        // WWDP EDIT END
        // Suffix:
        if (_random.Prob(component.ackChance))
        {
            msg += (" " + Loc.GetString("skeleton-suffix")); // e.g. "We only want to socialize. ACK ACK!"
        }
        return msg;
    }

    private void OnAccentGet(EntityUid uid, SkeletonAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
