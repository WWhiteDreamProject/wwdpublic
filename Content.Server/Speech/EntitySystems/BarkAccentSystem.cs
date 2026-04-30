using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class BarkAccentSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private static readonly IReadOnlyList<string> Barks = new List<string>{
            // WWDP EDIT START
            " Гав!", " ГАВ", " гав-гав"
            ////" Woof!", " WOOF", " wof-wof"
            // WWDP EDIT END
        }.AsReadOnly();

        private static readonly IReadOnlyDictionary<string, string> SpecialWords = new Dictionary<string, string>()
        {
            { "ah", "arf" },
            { "Ah", "Arf" },
            { "oh", "oof" },
            { "Oh", "Oof" },
            // WWDP EDIT START
            { "ах", "арф" },
            { "Ах", "Арф" },
            { "ох", "вуф" },
            { "Ох", "Вуф" },
            // WWDP EDIT END
        };

        public override void Initialize()
        {
            SubscribeLocalEvent<BarkAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            foreach (var (word, repl) in SpecialWords)
            {
                message = message.Replace(word, repl);
            }

            return message.Replace("!", _random.Pick(Barks))
                // WWDP EDIT START
                .Replace("л", "р").Replace("Л", "Р")
                // WWDP EDIT END
                .Replace("l", "r").Replace("L", "R");
        }

        private void OnAccent(EntityUid uid, BarkAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
