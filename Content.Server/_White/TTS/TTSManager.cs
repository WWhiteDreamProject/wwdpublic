using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Content.Shared._White;
using Prometheus;
using Robust.Shared.Configuration;

namespace Content.Server._White.TTS;

// ReSharper disable once InconsistentNaming
public sealed class TTSManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(
        "tts_req_timings",
        "Timings of TTS API requests",
        new HistogramConfiguration
        {
            LabelNames = new[] { "type" },
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });

    private static readonly Counter WantedCount =
        Metrics.CreateCounter("tts_wanted_count", "Amount of wanted TTS audio.");

    private static readonly Counter ReusedCount =
        Metrics.CreateCounter("tts_reused_count", "Amount of reused TTS audio from cache.");

    private static readonly Gauge CachedCount = Metrics.CreateGauge("tts_cached_count", "Amount of cached TTS audio.");

    private readonly HttpClient _httpClient = new();
    private readonly Dictionary<string, byte[]?> _cache = new();

    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
    }

    /// <summary>
    /// Generates audio with passed text by API
    /// </summary>
    /// <param name="speaker">Identifier of speaker</param>
    /// <param name="text">SSML formatted text</param>
    /// <returns>OGG audio bytes</returns>
    public async Task<byte[]?> ConvertTextToSpeech(string speaker, string text)
    {
        var url = _cfg.GetCVar(WhiteCVars.TTSApiUrl);
        if (string.IsNullOrWhiteSpace(url))
        {
            _sawmill.Log(LogLevel.Error, nameof(TTSManager), "TTS Api url not specified");
            return null;
        }

        var token = _cfg.GetCVar(WhiteCVars.TTSApiToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            _sawmill.Log(LogLevel.Error, nameof(TTSManager), "TTS Api token not specified");
            return null;
        }

        var maxCacheSize = _cfg.GetCVar(WhiteCVars.TTSMaxCache);
        WantedCount.Inc();
        var cacheKey = GenerateCacheKey(speaker, text);
        if (_cache.TryGetValue(cacheKey, out var data))
        {
            ReusedCount.Inc();
            _sawmill.Debug($"Use cached sound for '{text}' speech by '{speaker}' speaker");
            return data;
        }

        var body = new GenerateVoiceRequest
        {
            ApiToken = token,
            Text = text,
            Speaker = speaker
        };

        var reqTime = DateTime.UtcNow;
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.PostAsJsonAsync(url, body, cts.Token);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"TTS request returned bad status code: {response.StatusCode}");

            var soundData = await response.Content.ReadAsByteArrayAsync(cts.Token);

            if (_cache.Count > maxCacheSize)
                _cache.Remove(_cache.Last().Key);

            _cache.Add(cacheKey, soundData);
            CachedCount.Inc();

            _sawmill.Debug(
                $"Generated new sound for '{text}' speech by '{speaker}' speaker ({soundData.Length} bytes)");

            RequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);

            return soundData;
        }
        catch (TaskCanceledException)
        {
            RequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Warning($"Timeout of request generation new sound for '{text}' speech by '{speaker}' speaker");
            return null;
        }
        catch (Exception e)
        {
            RequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Warning($"Failed of request generation new sound for '{text}' speech by '{speaker}' speaker\n{e}");
            return null;
        }
    }

    public void ResetCache()
    {
        _cache.Clear();
        CachedCount.Set(0);
    }

    private string GenerateCacheKey(string speaker, string text)
    {
        var key = $"{speaker}/{text}";
        var keyData = Encoding.UTF8.GetBytes(key);
        var bytes = System.Security.Cryptography.SHA256.HashData(keyData);
        return Convert.ToHexString(bytes);
    }

    private record GenerateVoiceRequest
    {
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
