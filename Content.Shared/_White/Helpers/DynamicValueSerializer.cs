using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared._White.Helpers;

[TypeSerializer]
public sealed class DynamicValueSerializer : ITypeSerializer<DynamicValue, MappingDataNode>
{
    public DataNode Write(
        ISerializationManager serializationManager,
        DynamicValue value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null
        )
    {
        var type = GetType(serializationManager, new (value.Type));

        return new MappingDataNode
        {
            { "type", value.Type },
            { "value", serializationManager.WriteValue(type, value.Value) }
        };
    }

    public DynamicValue Read(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<DynamicValue>? instanceProvider = null
        )
    {
        var type = node.Get<ValueDataNode>("type");
        var value = serializationManager.Read(GetType(serializationManager, type), node.Get("value"), context)!;

        return new (type.Value, value);
    }

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null
    )
    {
        var nodes = new List<ValidationNode>();

        if (!node.Has("type"))
            nodes.Add(new ErrorNode(node, $"No type data node found"));

        if (!node.Has("value"))
            nodes.Add(new ErrorNode(node, $"No value data node found"));

        return new ValidatedSequenceNode(nodes);
    }

    private Type GetType(ISerializationManager serializationManager, ValueDataNode typeValue)
    {
        if (Type.GetType(typeValue.Value) is { } type)
            return type;

        type = serializationManager.Read<Type?>(typeValue);
        if (type is null)
            throw new InvalidMappingException("NO TYPE " + typeValue.Value);

        return type;
    }
}
