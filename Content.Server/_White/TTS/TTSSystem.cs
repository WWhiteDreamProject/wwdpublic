using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Server._White.Chat.Systems;
using Content.Shared._White;
using Content.Shared._White.TTS;
using Content.Shared.GameTicking;
using Content.Shared.Radio;
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

    private const int MaxMessageChars = 100 * 2; // same as SingleBubbleCharLimit * 2
    private bool _isEnabled = false;

    public override void Initialize()
    {
        _cfg.OnValueChanged(WhiteCVars.TTSEnabled, v => _isEnabled = v, true);
        _cfg.OnValueChanged(WhiteCVars.TTSAnnounceVoiceId, v => _voiceId = v, true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<AnnouncementSpokeEvent>(OnAnnouncementSpoke);
        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);
        SubscribeLocalEvent<TTSComponent, MapInitEvent>(OnTtsInitialized);
        SubscribeLocalEvent<TTSComponent, EntitySpokeLanguageEvent>(OnEntitySpoke);
    }

    private void OnTtsInitialized(Entity<TTSComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.VoicePrototypeId == null && _prototypeManager.TryGetRandom<TTSVoicePrototype>(_robustRandom, out var newTtsVoice))
        {
            ent.Comp.VoicePrototypeId = newTtsVoice.ID;
        }
    }


    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _ttsManager.ResetCache();
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeLanguageEvent args)
    {
        var voiceId = component.VoicePrototypeId;
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars ||
            voiceId == null)
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (voiceId == null || !_prototypeManager.TryIndex(voiceId.Value, out var protoVoice))
            return;

        if (args.IsWhisper)
        {
            if (args.OrgMsg.Count > 0 || args.ObsMsg.Count > 0)
            {
                if(args.OrgMsg.Count > 0)
                    HandleWhisper(uid, args.Message, args.ObfuscatedMessage!, protoVoice.Speaker, args.OrgMsg);
                if(args.ObsMsg.Count > 0 && args is { LangMessage: not null, ObfuscatedLangMessage: not null })
                    HandleWhisper(uid, args.LangMessage, args.ObfuscatedLangMessage, protoVoice.Speaker, args.ObsMsg);

                return;
            }
            HandleWhisper(uid, args.Message, args.ObfuscatedMessage, protoVoice.Speaker, null);

            return;
        }

        if (args.OrgMsg.Count > 0 || args.ObsMsg.Count > 0)
        {
            if(args.OrgMsg.Count > 0)
                HandleSay(uid, args.Message, protoVoice.Speaker, args.OrgMsg);
            if(args.ObsMsg.Count > 0)
                HandleSay(uid, args.ObfuscatedMessage, protoVoice.Speaker, args.ObsMsg);
            return;
        }
        HandleSay(uid, args.Message, protoVoice.Speaker, null);
    }

    private async void HandleSay(EntityUid uid, string message, string speaker, Filter? filter)
    {
        var soundData = await GenerateTTS(message, speaker);
        if (soundData is null) return;
        RaiseNetworkEvent(new PlayTTSEvent(soundData, GetNetEntity(uid)), filter ?? Filter.Pvs(uid));
    }

    private async void HandleWhisper(EntityUid uid, string message, string obfMessage, string speaker, Filter? filter)
    {
        var netEntity = GetNetEntity(uid);

        PlayTTSEvent fullTtsEvent;
        PlayTTSEvent? obfTtsEvent = null;

        {
            var fullSoundData = await GenerateTTS(message, speaker, true);
            if (fullSoundData is null)
                return;

            fullTtsEvent = new PlayTTSEvent(fullSoundData, netEntity, true);
            if (message == obfMessage)
            {
                obfTtsEvent = fullTtsEvent;
            }
            else
            {
                var obfSoundData = await GenerateTTS(obfMessage, speaker, true);
                if (obfSoundData is not null)
                {
                    obfTtsEvent = new PlayTTSEvent(obfSoundData, netEntity, true);
                }
            }
        }

        // TODO: Check obstacles
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);
        var receptions = (filter ?? Filter.Pvs(uid)).Recipients;
        foreach (var session in receptions)
        {
            if (!xformQuery.TryComp(session.AttachedEntity, out var xform))
                continue;

            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();
            if (distance > ChatSystem.VoiceRange * ChatSystem.VoiceRange)
                continue;

            if(distance <= ChatSystem.WhisperClearRange)
                RaiseNetworkEvent(fullTtsEvent, session);
            else if(obfTtsEvent!= null)
                RaiseNetworkEvent(obfTtsEvent, session);
        }
    }


    private readonly Dictionary<string, Task<byte[]?>> _ttsTasks = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    // ReSharper disable once InconsistentNaming
    private async Task<byte[]?> GenerateTTS(string text, string speaker, bool isWhisper = false)
    {
        var textSanitized = Sanitize(text);
        if (textSanitized == "") return null;
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
            {
                return await existingTask;
            }

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

public sealed class EntitySpokeLanguageEvent: EntityEventArgs
{
    public readonly string? ObfuscatedLangMessage;
    public readonly string? LangMessage;
    public readonly bool IsWhisper;
    public readonly Filter OrgMsg;
    public readonly Filter ObsMsg;
    public readonly EntityUid Source;
    public readonly string Message;
    public readonly string OriginalMessage;
    public readonly string ObfuscatedMessage; // not null if this was a whisper

    /// <summary>
    ///     If the entity was trying to speak into a radio, this was the channel they were trying to access. If a radio
    ///     message gets sent on this channel, this should be set to null to prevent duplicate messages.
    /// </summary>
    public RadioChannelPrototype? Channel;

    public EntitySpokeLanguageEvent(
        Filter orgMsg,
        Filter obsMsg,
        EntityUid source,
        string message,
        string originalMessage,
        RadioChannelPrototype? channel,
        bool isWhisper,
        string obfuscatedMessage,
        string? langMessage = null,
        string? obfuscatedLangMessage = null)
    {
        ObfuscatedLangMessage = obfuscatedLangMessage;
        LangMessage = langMessage;
        IsWhisper = isWhisper;
        OrgMsg = orgMsg;
        ObsMsg = obsMsg;
        Source = source;
        Message = message;
        OriginalMessage = originalMessage;
        Channel = channel;
        ObfuscatedMessage = obfuscatedMessage;
    }
}

public sealed class TransformSpeakerVoiceEvent : EntityEventArgs
{
    public EntityUid Sender;
    public string VoiceId;

    public TransformSpeakerVoiceEvent(EntityUid sender, string voiceId)
    {
        Sender = sender;
        VoiceId = voiceId;
    }
}
