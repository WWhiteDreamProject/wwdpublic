using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._White;
using Prometheus;
using Robust.Shared.Configuration;

namespace Content.Server._White.TTS;

// ReSharper disable once InconsistentNaming
public static class TTSManagerExtension
{
    private static readonly Histogram AnnounceRequestTimings = Metrics.CreateHistogram(
        "tts_announce_req_timings",
        "Timings announce of TTS API requests",
        new HistogramConfiguration()
        {
            LabelNames = new[] {"type"},
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });

    private static readonly Counter AnnounceWantedCount = Metrics.CreateCounter(
        "tts_announce_wanted_count",
        "Amount announce of wanted TTS audio.");

    private static readonly Counter AnnounceReusedCount = Metrics.CreateCounter(
        "tts_announce_reused_count",
        "Amount announce of reused TTS audio from cache.");

    private static readonly Histogram RadioRequestTimings = Metrics.CreateHistogram(
        "tts_radio_req_timings",
        "Timings radio of TTS API requests",
        new HistogramConfiguration()
        {
            LabelNames = new[] {"type"},
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });

    private static readonly Counter RadioWantedCount = Metrics.CreateCounter(
        "tts_radio_wanted_count",
        "Amount radio of wanted TTS audio.");

    private static readonly Counter RadioReusedCount = Metrics.CreateCounter(
        "tts_radio_reused_count",
        "Amount radio of reused TTS audio from cache.");

    private static readonly HttpClient _httpClient = new();

    public static async Task<byte[]> RadioConvertTextToSpeech(this TTSManager _cfTtsManager, string speaker, string text)
    {
        // ReSharper disable once InconsistentNaming
        var _sawmill = Logger.GetSawmill("tts");
        // ReSharper disable once InconsistentNaming
        var _cfg = IoCManager.Resolve<IConfigurationManager>();

        var url = _cfg.GetCVar(WhiteCVars.TTSApiUrl);
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new Exception("TTS Api url not specified");
        }

        var token = _cfg.GetCVar(WhiteCVars.TTSApiToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new Exception("TTS Api token not specified");
        }

        RadioWantedCount.Inc();
        var cacheKey = GenerateCacheKey(speaker, text, "echo");
        if (_cfTtsManager._cache.TryGetValue(cacheKey, out var data))
        {
            RadioReusedCount.Inc();
            _sawmill.Debug($"Use cached radio sound for '{text}' speech by '{speaker}' speaker");
            return data;
        }

        var body = new GenerateVoiceRequest
        {
            ApiToken = token,
            Text = text,
            Speaker = speaker,
            Effect = "Radio"
        };

        var reqTime = DateTime.UtcNow;
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            var response = await _httpClient.PostAsJsonAsync(url, body, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"TTS request returned bad status code: {response.StatusCode}");
            }

            var json = await response.Content.ReadFromJsonAsync<GenerateVoiceResponse>();
            var soundData = Convert.FromBase64String(json.Results.First().Audio);

            _cfTtsManager._cache.Add(cacheKey, soundData);
            _cfTtsManager._cacheKeysSeq.Add(cacheKey);

            _sawmill.Debug($"Generated new radio sound for '{text}' speech by '{speaker}' speaker ({soundData.Length} bytes)");
            RadioRequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);

            return soundData;
        }
        catch (TaskCanceledException)
        {
            RadioRequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Error($"Timeout of request generation new radio sound for '{text}' speech by '{speaker}' speaker");
            throw new Exception("TTS request timeout");
        }
        catch (Exception e)
        {
            RadioRequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Error($"Failed of request generation new radio sound for '{text}' speech by '{speaker}' speaker\n{e}");
            throw new Exception("TTS request failed");
        }
    }
    public static async Task<byte[]> AnnounceConvertTextToSpeech(this TTSManager _cfTtsManager, string speaker, string text)
    {
        // ReSharper disable once InconsistentNaming
        var _sawmill = Logger.GetSawmill("tts");
        // ReSharper disable once InconsistentNaming
        var _cfg = IoCManager.Resolve<IConfigurationManager>();

        var url = _cfg.GetCVar(WhiteCVars.TTSApiUrl);
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new Exception("TTS Api url not specified");
        }

        var token = _cfg.GetCVar(WhiteCVars.TTSApiToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new Exception("TTS Api token not specified");
        }

        AnnounceWantedCount.Inc();
        var cacheKey = GenerateCacheKey(speaker, text, "echo");
        if (_cfTtsManager._cache.TryGetValue(cacheKey, out var data))
        {
            AnnounceReusedCount.Inc();
            _sawmill.Debug($"Use cached announce sound for '{text}' speech by '{speaker}' speaker");
            return data;
        }

        var body = new GenerateVoiceRequest
        {
            ApiToken = token,
            Text = text,
            Speaker = speaker,
            Effect = "Echo"
        };

        var reqTime = DateTime.UtcNow;
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var response = await _httpClient.PostAsJsonAsync(url, body, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"TTS request returned bad status code: {response.StatusCode}");
            }

            var json = await response.Content.ReadFromJsonAsync<GenerateVoiceResponse>();
            var soundData = Convert.FromBase64String(json.Results.First().Audio);

            _cfTtsManager._cache.Add(cacheKey, soundData);
            _cfTtsManager._cacheKeysSeq.Add(cacheKey);

            _sawmill.Debug($"Generated new announce sound for '{text}' speech by '{speaker}' speaker ({soundData.Length} bytes)");
            AnnounceRequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);

            return soundData;
        }
        catch (TaskCanceledException)
        {
            AnnounceRequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Error($"Timeout of request generation new announce sound for '{text}' speech by '{speaker}' speaker");
            throw new Exception("TTS request timeout");
        }
        catch (Exception e)
        {
            AnnounceRequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Error($"Failed of request generation new announce sound for '{text}' speech by '{speaker}' speaker\n{e}");
            throw new Exception("TTS request failed", e);
        }
    }

    private static string GenerateCacheKey(string speaker, string text, string effect = "")
    {
        var key = $"{speaker}/{text}/{effect}";
        byte[] keyData = Encoding.UTF8.GetBytes(key);
        var bytes = System.Security.Cryptography.SHA1.HashData(keyData);
        return Convert.ToHexString(bytes);
    }

    private struct GenerateVoiceRequest
    {
        public GenerateVoiceRequest()
        {
        }

        [JsonPropertyName("api_token")]
        public string ApiToken { get; set; } = "";

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("speaker")]
        public string Speaker { get; set; } = "";

        [JsonPropertyName("ssml")]
        public bool SSML { get; private set; } = true;

        [JsonPropertyName("word_ts")]
        public bool WordTS { get; private set; } = false;

        [JsonPropertyName("put_accent")]
        public bool PutAccent { get; private set; } = true;

        [JsonPropertyName("put_yo")]
        public bool PutYo { get; private set; } = false;

        [JsonPropertyName("sample_rate")]
        public int SampleRate { get; private set; } = 24000;

        [JsonPropertyName("format")]
        public string Format { get; private set; } = "ogg";

        [JsonPropertyName("effect")]
        public string Effect { get; set; } = "none";
    }

    private struct GenerateVoiceResponse
    {
        [JsonPropertyName("results")]
        public List<VoiceResult> Results { get; set; }

        [JsonPropertyName("original_sha1")]
        public string Hash { get; set; }
    }

    private struct VoiceResult
    {
        [JsonPropertyName("audio")]
        public string Audio { get; set; }
    }
}
