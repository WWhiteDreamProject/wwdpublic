using System.Globalization;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;


namespace Content.Shared._NC.Trade;


[Serializable, NetSerializable,]
public readonly record struct IntRange(int Min, int Max)
{
    public static IntRange Fixed(int value) => new(value, value);

    public static IntRange Create(int min, int max)
    {
        if (max < min)
            (min, max) = (max, min);

        return new(min, max);
    }
}

[TypeSerializer]
public sealed class IntRangeSerializer :
    ITypeSerializer<IntRange, ValueDataNode>,
    ITypeSerializer<IntRange, MappingDataNode>
{
    public ValidationNode Validate(
            ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null
        ) =>
            serializationManager.ValidateNode<Dictionary<string, int>>(node, context);

    public IntRange Read(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<IntRange>? instanceProvider = null
    )
    {
        int? min = null;
        int? max = null;

        static bool TryGetInt(MappingDataNode node, string key, out int value)
        {
            value = 0;
            return node.TryGet(key, out var n) &&
                n is ValueDataNode v &&
                int.TryParse(v.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        if (TryGetInt(node, "min", out var parsedMin) || TryGetInt(node, "Min", out parsedMin))
            min = parsedMin;

        if (TryGetInt(node, "max", out var parsedMax) || TryGetInt(node, "Max", out parsedMax))
            max = parsedMax;

        if (min is null && max is null)
            throw new InvalidOperationException("IntRange mapping must contain 'min' and/or 'max'.");

        var a = min ?? max!.Value;
        var b = max ?? min!.Value;

        return IntRange.Create(a, b);
    }

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null
    ) =>
        serializationManager.ValidateNode<int>(node, context);

    public IntRange Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<IntRange>? instanceProvider = null
    )
    {
        if (!int.TryParse(node.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            throw new InvalidOperationException($"Invalid IntRange value '{node.Value}', expected integer.");

        return IntRange.Fixed(v);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        IntRange value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null
    )
    {
        if (value.Min == value.Max)
            return new ValueDataNode(value.Min.ToString(CultureInfo.InvariantCulture));

        var map = new MappingDataNode();
        map.Add("min", new ValueDataNode(value.Min.ToString(CultureInfo.InvariantCulture)));
        map.Add("max", new ValueDataNode(value.Max.ToString(CultureInfo.InvariantCulture)));
        return map;
    }
}
