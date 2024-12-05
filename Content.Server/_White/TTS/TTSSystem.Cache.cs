using System.Threading.Tasks;
using Content.Shared.Corvax.TTS;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

// ReSharper disable once CheckNamespace
namespace Content.Server.Corvax.TTS;

public sealed partial class TTSSystem
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;

    private ResPath GetCacheId(TTSVoicePrototype voicePrototype, string cacheId)
    {
        var resPath = new ResPath($"voicecache/{voicePrototype.ID}/{cacheId}.ogg").ToRootedPath();
        _resourceManager.UserData.CreateDir(resPath.Directory);
        return resPath.ToRootedPath();
    }
    private async Task<byte[]?> GetFromCache(ResPath resPath)
    {
        if (!_resourceManager.UserData.Exists(resPath))
        {
            return null;
        }

        await using var reader = _resourceManager.UserData.OpenRead(resPath);
        return reader.CopyToArray();
    }

    private async Task SaveVoiceCache(ResPath resPath, byte[] data)
    {
        await using var writer = _resourceManager.UserData.OpenWrite(resPath);
        await writer.WriteAsync(data);
    }


}
