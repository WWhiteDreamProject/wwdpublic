using System.Diagnostics.CodeAnalysis;
using System.IO;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;


namespace Content.Client._White.DatumContainer;


public sealed class LocalDatumContainer<T> where T : notnull
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    private readonly ResPath _datumPath;
    private readonly ResPath _rootPath = new ResPath("/Datum/");

    private Dictionary<string, T> _data = new();

    public LocalDatumContainer(string datumName)
    {
        IoCManager.InjectDependencies(this);
        _datumPath = _rootPath / new ResPath($"{datumName}.yaml");
        LoadDataFromUserData();
    }

    public bool TryGetValue(string key,[NotNullWhen(true)] out T? value) =>
        _data.TryGetValue(key, out value);

    public void SetValue(string key, T? value)
    {
        if (value is null)
        {
            RemoveValue(key);
            return;
        }

        _data[key] = value;
        Dirty();
    }

    public void RemoveValue(string key)
    {
        _data.Remove(key);
        Dirty();
    }

    private void Dirty()
    {
        var rootNode = _serializationManager.WriteValue(_data, notNullableOverride:true);
        using var stream = _resourceManager.UserData.Open(_datumPath, FileMode.Create);
        using var textWriter = new StreamWriter(stream);
        rootNode.Write(textWriter);
    }

    private void LoadDataFromUserData()
    {
        if(!_resourceManager.UserData.IsDir(_rootPath))
            _resourceManager.UserData.CreateDir(_rootPath);

        if (!_resourceManager.UserData.Exists(_datumPath))
            return;

        using var stream = _resourceManager.UserData.Open(_datumPath, FileMode.Open);
        using var textReadStream = new StreamReader(stream);
        var yamlStream = new YamlStream();
        yamlStream.Load(textReadStream);

        _data = _serializationManager.Read<Dictionary<string, T>>(yamlStream.Documents[0].RootNode.ToDataNode(), notNullableOverride:false);
    }

    public T? GetValueOrDefault(string key) => _data.GetValueOrDefault(key);
}
