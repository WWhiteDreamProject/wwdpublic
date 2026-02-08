using System.Threading;
using System.Threading.Tasks;
using Content.Server._White.Hearing;
using Content.Server.Chat.Systems;
using Content.Server.Language;
using Content.Shared._White.CCVar;
using Content.Shared._White.TTS;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Language;
using Content.Shared.Language.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._White.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly LanguageSystem _language = default!;

    private const int MaxMessageChars = 100 * 2; // same as SingleBubbleCharLimit * 2
    private bool _isEnabled;

    public override void Initialize()
    {
        _cfg.OnValueChanged(WhiteCVars.TTSEnabled, v => _isEnabled = v, true);

        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => _ttsManager.ResetCache());

        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        if (!_isEnabled || args.Message.Length > MaxMessageChars)
            return;

        if (!args.Language.SpeechOverride.RequireSpeech) // e.g. Sign language
            return;

        var voiceId = component.VoicePrototypeId;
        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!_prototypeManager.TryIndex(voiceId, out var protoVoice))
            return;

        if (args.IsWhisper)
        {
            HandleWhisper(uid, args.Message, args.Language, protoVoice.Speaker);
            return;
        }

        HandleSay(uid, args.Message, args.Language, protoVoice.Speaker);
    }

    private async void HandleSay(EntityUid uid, string message, LanguagePrototype language, string speaker)
    {
        var normal = await GenerateTTS(message, speaker);
        if (normal is null)
            return;

        var obfuscated = await GenerateTTS(_language.ObfuscateSpeech(message, language), speaker);
        if (obfuscated is null)
            return;

        var nilter = Filter.Empty();
        var lilter = Filter.Empty();
        foreach (var session in Filter.Pvs(uid).Recipients)
        {
            if (!session.AttachedEntity.HasValue)
                continue;

            if (EntityManager.HasComponent<DeafComponent>(session.AttachedEntity.Value))
                continue;

            EntityManager.TryGetComponent(session.AttachedEntity.Value, out LanguageSpeakerComponent? lang);
            if (_language.CanUnderstand(new(session.AttachedEntity.Value, lang), language.ID))
                nilter.AddPlayer(session);
            else
                lilter.AddPlayer(session);
        }

        RaiseNetworkEvent(new PlayTTSEvent(normal, GetNetEntity(uid)), nilter);
        RaiseNetworkEvent(new PlayTTSEvent(obfuscated, GetNetEntity(uid)), lilter, false);
    }

    private async void HandleWhisper(EntityUid uid, string message, LanguagePrototype language, string speaker)
    {
        var normal = await GenerateTTS(message, speaker, true);
        if (normal is null)
            return;

        var obfuscated = await GenerateTTS(_language.ObfuscateSpeech(message, language), speaker, true);
        if (obfuscated is null)
            return;

        // TODO: Check obstacles
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);
        var nilter = Filter.Empty();
        var lilter = Filter.Empty();
        foreach (var session in Filter.Pvs(uid).Recipients)
        {
            if (!session.AttachedEntity.HasValue)
                continue;

            var xform = xformQuery.GetComponent(session.AttachedEntity.Value);
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();
            if (distance > ChatSystem.WhisperMuffledRange)
                continue;

            EntityManager.TryGetComponent(session.AttachedEntity.Value, out LanguageSpeakerComponent? lang);
            if (_language.CanUnderstand(new(session.AttachedEntity.Value, lang), language.ID)
                && distance <= ChatSystem.WhisperClearRange)
                nilter.AddPlayer(session);
            else
                lilter.AddPlayer(session);
        }

        RaiseNetworkEvent(new PlayTTSEvent(normal, GetNetEntity(uid), true), nilter);
        RaiseNetworkEvent(new PlayTTSEvent(obfuscated, GetNetEntity(uid), true), lilter, false);
    }

    private readonly Dictionary<string, Task<byte[]?>> _ttsTasks = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    // ReSharper disable once InconsistentNaming
    private async Task<byte[]?> GenerateTTS(string text, string speaker, bool isWhisper = false)
    {
        var textSanitized = Sanitize(text);
        if (string.IsNullOrEmpty(textSanitized))
            return null;

        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        var ssmlTraits = SoundTraits.RateFast;
        if (isWhisper)
            ssmlTraits = SoundTraits.PitchVerylow;

        var textSsml = ToSsmlText(textSanitized, ssmlTraits);

        // Создаем уникальный ключ на основе всех аргументов
        var taskKey = $"{textSanitized}_{speaker}_{isWhisper}";

        // Блокируем доступ к словарю, чтобы избежать гонки
        await _lock.WaitAsync();
        try
        {
            // Если задача уже выполняется для этого набора аргументов, ждем её завершения
            if (_ttsTasks.TryGetValue(taskKey, out var existingTask))
                return await existingTask;

            // Создаем задачу и сохраняем её в словарь
            var newTask = _ttsManager.ConvertTextToSpeech(speaker, textSsml);
            _ttsTasks[taskKey] = newTask;
        }
        finally
        {
            _lock.Release();
        }

        try
        {
            // Ожидаем завершения задачи
            return await _ttsTasks[taskKey];
        }
        finally
        {
            // Удаляем задачу из словаря независимо от результата
            await _lock.WaitAsync();
            try
            {
                _ttsTasks.Remove(taskKey);
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}

public sealed class TransformSpeakerVoiceEvent(EntityUid sender, string voiceId) : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
    public EntityUid Sender = sender;
    public string VoiceId = voiceId;
}
