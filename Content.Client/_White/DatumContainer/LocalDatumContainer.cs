using System.Diagnostics.CodeAnalysis;
using System.IO;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;


namespace Content.Client._White.DatumContainer;


public sealed class LocalDatumContainer<T> where T : notnull
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    private readonly ResPath _datumPath;
    private readonly ResPath _rootPath = new ResPath("/Datum/");

    private Dictionary<string, T> _data = new();

    private IWritableDirProvider UserData => _resourceManager.UserData;

    public LocalDatumContainer(string datumName)
    {
        IoCManager.InjectDependencies(this);
        _datumPath = _rootPath / new ResPath(datumName + ".yaml");
        LoadDataFromUserData();
    }

    public bool TryGetValue(string key,[NotNullWhen(true)] out T? value) =>
        _data.TryGetValue(key, out value);

    public void SetValue(string key, T value)
    {
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
        using var stream = UserData.Open(_datumPath, FileMode.Create);
        using var textWriter = new StreamWriter(stream);
        rootNode.Write(textWriter);
    }

    private void LoadDataFromUserData()
    {
        if(!UserData.Exists(_rootPath))
            UserData.CreateDir(_rootPath);

        if (!UserData.Exists(_datumPath))
            return;

        var stream = _resourceManager.ContentFileReadYaml(_datumPath);
        _data = _serializationManager.Read<Dictionary<string, T>>(stream.Documents[0].RootNode.ToDataNode(), notNullableOverride:true);
    }
}
