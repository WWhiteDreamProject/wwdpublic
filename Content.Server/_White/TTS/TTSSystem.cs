using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Server.Light.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._White;
using Content.Shared._White.TTS;
using Content.Shared._White.TTS.Events;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._White.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly TTSPitchRateSystem _ttsPitchRateSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private const int MaxMessageChars = 100 * 2; // same as SingleBubbleCharLimit * 2
    private bool _isEnabled;
    private string _apiUrl = string.Empty;

    private readonly string[] _whisperWords = ["тсс", "псс", "ччч", "ссч", "сфч", "тст"];

    public override void Initialize()
    {
        _cfg.OnValueChanged(WhiteCVars.TTSEnabled, v => _isEnabled = v, true);
        _cfg.OnValueChanged(WhiteCVars.TTSApiUrl, url => _apiUrl = url, true);

        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);

        SubscribeLocalEvent<TtsAnnouncementEvent>(OnAnnounceRequest);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        _netMgr.RegisterNetMessage<MsgRequestTTS>(OnRequestTTS);
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        if (!_isEnabled || string.IsNullOrEmpty(_apiUrl) || args.Message.Length > MaxMessageChars)
            return;

        var voiceId = component.Prototype;
        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!_prototypeManager.TryIndex(voiceId, out var protoVoice))
            return;

        var message = FormattedMessage.RemoveMarkup(args.Message);

        var soundData = await GenerateTTS(uid, message, protoVoice.Speaker);
        if (soundData is null)
            return;

        var ttsEvent = new PlayTTSEvent(GetNetEntity(uid), soundData, false);

        // Say
        if (!args.IsWhisper)
        {
            RaiseNetworkEvent(ttsEvent, Filter.Pvs(uid), false);
            return;
        }

        // Whisper
        var chosenWhisperText = _random.Pick(_whisperWords);
        var obfSoundData = await GenerateTTS(uid, chosenWhisperText, protoVoice.Speaker);
        if (obfSoundData is null)
            return;
        var obfTtsEvent = new PlayTTSEvent(GetNetEntity(uid), obfSoundData, false);
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);
        var receptions = Filter.Pvs(uid).Recipients;

        foreach (var session in receptions)
        {
            if (!session.AttachedEntity.HasValue)
                continue;
            var xform = xformQuery.GetComponent(session.AttachedEntity.Value);
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).LengthSquared();
            if (distance > ChatSystem.VoiceRange * ChatSystem.VoiceRange)
                continue;

            EntityEventArgs actualEvent = distance > ChatSystem.WhisperClearRange
                ? obfTtsEvent
                : ttsEvent;

            RaiseNetworkEvent(actualEvent, Filter.SinglePlayer(session), false);
        }
    }

    private async void OnAnnounceRequest(TtsAnnouncementEvent ev)
    {
        if (!_prototypeManager.TryIndex(ev.VoiceId, out var ttsPrototype))
            return;
        var message = FormattedMessage.RemoveMarkup(ev.Message);
        var soundData = await GenerateTTS(null, message, ttsPrototype.Speaker, speechRate: "slow", effect: "announce");
        if (soundData == null)
            return;
        Filter filter;
        if (ev.Global)
            filter = Filter.Broadcast();
        else
        {
            var station = _stationSystem.GetOwningStation(ev.Source);
            if (station == null || !EntityManager.TryGetComponent<StationDataComponent>(station, out var stationDataComp))
                return;

            filter = _stationSystem.GetInStation(stationDataComp);
        }

        foreach (var player in filter.Recipients)
        {
            if (player.AttachedEntity == null)
                continue;

            // Get emergency lights in range to broadcast from
            var entities = _lookup.GetEntitiesInRange(player.AttachedEntity.Value, 30f)
                .Where(HasComp<EmergencyLightComponent>)
                .ToList();

            if (entities.Count == 0)
                return;

            // Get closest emergency light
            var entity = entities.First();
            var range = new Vector2(100f);

            foreach (var item in entities)
            {
                var itemSource = _xforms.GetWorldPosition(Transform(item));
                var playerSource = _xforms.GetWorldPosition(Transform(player.AttachedEntity.Value));

                var distance = playerSource - itemSource;

                if (range.Length() <= distance.Length())
                    continue;

                range = distance;
                entity = item;
            }

            RaiseNetworkEvent(new PlayTTSEvent(GetNetEntity(entity), soundData, true), Filter.SinglePlayer(player),
                false);
        }
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _ttsManager.ResetCache();
    }

    private async void OnRequestTTS(MsgRequestTTS ev)
    {
        var url = _cfg.GetCVar(WhiteCVars.TTSApiUrl);
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (!_playerManager.TryGetSessionByChannel(ev.MsgChannel, out var session) ||
            !_prototypeManager.TryIndex(ev.VoiceId, out var protoVoice))
            return;

        var soundData = await GenerateTTS(GetEntity(ev.Uid), ev.Text, protoVoice.Speaker);
        if (soundData != null)
            RaiseNetworkEvent(new PlayTTSEvent(ev.Uid, soundData, false), Filter.SinglePlayer(session), false);
    }

    private async Task<byte[]?> GenerateTTS(EntityUid? uid, string text, string speaker, string? speechPitch = null,
        string? speechRate = null, string? effect = null)
    {
        var textSanitized = Sanitize(text);
        if (textSanitized == "")
            return null;

        string pitch;
        string rate;
        if (speechPitch == null || speechRate == null)
        {
            if (uid == null || !_ttsPitchRateSystem.TryGetPitchRate(uid.Value, out var pitchRate))
            {
                pitch = "medium";
                rate = "medium";
            }
            else
            {
                pitch = pitchRate.Pitch;
                rate = pitchRate.Rate;
            }
        }
        else
        {
            pitch = speechPitch;
            rate = speechRate;
        }

        return await _ttsManager.ConvertTextToSpeech(speaker, textSanitized, pitch, rate, effect);
    }
}

public sealed class TransformSpeakerVoiceEvent(EntityUid sender, string voiceId) : EntityEventArgs
{
    public EntityUid Sender = sender;
    public ProtoId<TTSVoicePrototype> VoiceId = voiceId;
}

public sealed class TtsAnnouncementEvent(string message, string voiceId, EntityUid source, bool global) : EntityEventArgs
{
    public readonly string Message = message;
    public readonly bool Global = global;
    public readonly ProtoId<TTSVoicePrototype> VoiceId = voiceId;
    public readonly EntityUid Source = source;
}
