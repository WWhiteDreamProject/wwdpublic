using System.Threading.Tasks;
using Content.Server._White.Chat.Systems;
using Content.Shared._White.TTS;

namespace Content.Server._White.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem
{
    private string _voiceId = "Announcer";

    private async void OnAnnouncementSpoke(AnnouncementSpokeEvent args)
    {
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars * 2 ||
            !_prototypeManager.TryIndex<TTSVoicePrototype>(_voiceId, out var protoVoice))
        {
            RaiseNetworkEvent(new AnnounceTTSEvent(new byte[]{}, args.AnnouncementSound, args.AnnouncementSoundParams), args.Source);
            return;
        }

        byte[]? soundData = null;
        try
        {
            soundData = await GenerateTtsAnnouncement(args.Message, protoVoice.Speaker);

        }
        catch (Exception)
        {
            // skip!
        }
        soundData ??= new byte[] { };
        RaiseNetworkEvent(new AnnounceTTSEvent(soundData, args.AnnouncementSound, args.AnnouncementSoundParams), args.Source);
    }

    private async Task<byte[]?> GenerateTtsAnnouncement(string text, string speaker)
    {
        var textSanitized = Sanitize(text);
        if (textSanitized == "") return null;
        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        var textSsml = ToSsmlText(textSanitized, SoundTraits.RateFast);

        var position = textSsml.LastIndexOf("Отправитель", StringComparison.InvariantCulture);
        if (position != -1)
        {
            textSsml = textSsml[..position] + "<break time='33ms'/>" + textSsml[position..];
        }
        return await _ttsManager.AnnounceConvertTextToSpeech(speaker, textSsml);
    }
}
