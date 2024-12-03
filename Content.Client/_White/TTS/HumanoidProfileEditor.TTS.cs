using System.Linq;
using Content.Client._White.TTS;
using Content.Shared.Preferences;
using Content.Shared._White.TTS;
using Robust.Shared.Random;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private TTSSystem _ttsSystem = default!;
    private TTSManager _ttsManager = default!;
    private IRobustRandom _random = default!;

    private List<TTSVoicePrototype> _voiceList = default!;

    private readonly string[] _sampleText =
    [
        "Помогите, клоун насилует в технических тоннелях!",
        "ХоС, ваши сотрудники украли у меня собаку и засунули ее в стиральную машину!",
        "Агент синдиката украл пиво из бара и взорвался!",
        "Врача! Позовите врача!"
    ];

    private void InitializeVoice()
    {
        _random = IoCManager.Resolve<IRobustRandom>();
        _ttsManager = IoCManager.Resolve<TTSManager>();
        _ttsSystem = IoCManager.Resolve<IEntityManager>().System<TTSSystem>();
        _voiceList = _prototypeManager.EnumeratePrototypes<TTSVoicePrototype>().Where(o => o.RoundStart).ToList();

        VoiceButton.OnItemSelected += args =>
        {
            VoiceButton.SelectId(args.Id);
            SetVoice(_voiceList[args.Id].ID);
        };

        VoicePlayButton.OnPressed += _ => { PlayTTS(); };
    }

    private void UpdateTTSVoicesControls()
    {
        if (Profile is null)
            return;

        VoiceButton.Clear();

        var firstVoiceChoiceId = 1;
        for (var i = 0; i < _voiceList.Count; i++)
        {
            var voice = _voiceList[i];
            if (!HumanoidCharacterProfile.CanHaveVoice(voice, Profile.Sex))
                continue;

            var name = Loc.GetString(voice.Name);
            VoiceButton.AddItem(name, i);

            if (firstVoiceChoiceId == 1)
                firstVoiceChoiceId = i;
        }

        var voiceChoiceId = _voiceList.FindIndex(x => x.ID == Profile.Voice);
        if (!VoiceButton.TrySelectId(voiceChoiceId) && VoiceButton.TrySelectId(firstVoiceChoiceId))
            SetVoice(_voiceList[firstVoiceChoiceId].ID);
    }

    private void PlayTTS()
    {
        if (Profile is null)
            return;

        _ttsSystem.StopCurrentTTS(PreviewDummy);
        _ttsManager.RequestTTS(PreviewDummy, _random.Pick(_sampleText), Profile.Voice);
    }
}
