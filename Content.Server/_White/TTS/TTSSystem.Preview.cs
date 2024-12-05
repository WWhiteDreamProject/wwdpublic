using Content.Shared.Backmen.TTS;
using Content.Shared.Corvax.TTS;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Player;
using Robust.Shared.Random;


// ReSharper disable once CheckNamespace
namespace Content.Server.Corvax.TTS;

public sealed partial class TTSSystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    private readonly List<string> _sampleText = new() // TODO: Локализация?
    {
        "Съешь же ещё этих мягких французских булок, да выпей чаю.",
        "Клоун, прекрати разбрасывать банановые кожурки офицерам под ноги!",
        "Капитан, вы уверены что хотите назначить клоуна на должность главы персонала?",
        "Эс Бэ! Тут человек в сером костюме, с тулбоксом и в маске! Помогите!!",
        "Учёные, тут странная аномалия в баре! Она уже съела мима!",
        "Я надеюсь что инженеры внимательно следят за сингулярностью...",
        "Вы слышали эти странные крики в техах? Мне кажется туда ходить небезопасно.",
        "Вы не видели Гамлета? Мне кажется он забегал к вам на кухню.",
        "Здесь есть доктор? Человек умирает от отравленного пончика! Нужна помощь!",
        "Вам нужно согласие и печать квартирмейстера, если вы хотите сделать заказ на партию дробовиков.",
        "Возле эвакуационного шаттла разгерметизация! Инженеры, нам срочно нужна ваша помощь!",
        "Бармен, налей мне самого крепкого вина, которое есть в твоих запасах!"
    };

    /// <summary>
    /// Вообще не понимаю на какой хрен позволять пользователяем ддосить сервер ттса да и еще своим любым текстом -_-
    /// </summary>
    /// <param name="ev"></param>
    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled ||
            !_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
            return;

        var txt = _robustRandom.Pick(_sampleText);
        var cacheId = GetCacheId(protoVoice, $"{VoiceRequestType.Preview.ToString()}-{_sampleText.IndexOf(txt)}");

        var cached = await GetFromCache(cacheId);
        if (cached != null)
        {
            RaiseNetworkEvent(new PlayTTSEvent(cached), Filter.SinglePlayer(args.SenderSession));
            return;
        }

        if (HandleRateLimit(args.SenderSession) != RateLimitStatus.Allowed)
            return;

        var soundData = await GenerateTTS(txt, protoVoice.Speaker);
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData), Filter.SinglePlayer(args.SenderSession), false); // not record for replay

        await SaveVoiceCache(cacheId, soundData);
    }
}
