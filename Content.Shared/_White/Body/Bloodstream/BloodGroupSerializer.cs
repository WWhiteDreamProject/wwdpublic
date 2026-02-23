using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared._White.Body.Bloodstream;

[TypeSerializer]
public sealed class BloodGroupSerializer : ITypeReader<BloodGroup, MappingDataNode>, ITypeCopier<BloodGroup>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null
        )
    {
        var nodes = new List<ValidationNode>();

        if (node.TryGet("type", out ValueDataNode? type)
            && !Enum.TryParse(type.Value, out BloodType _))
            nodes.Add(new ErrorNode(type, $"Can't parse {type.Value} to {typeof(BloodType)}"));

        if (node.TryGet("rhesusFactor", out ValueDataNode? rhesusFactor)
            && !Enum.TryParse(rhesusFactor.Value, out BloodRhesusFactor _))
            nodes.Add(new ErrorNode(rhesusFactor, $"Can't parse {rhesusFactor.Value} to {typeof(BloodRhesusFactor)}"));

        return new ValidatedSequenceNode(nodes);
    }

    public BloodGroup Read(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<BloodGroup>? instanceProvider = null
        )
    {
        var bloodGroup = new BloodGroup();

        if (node.TryGet("type", out ValueDataNode? type)
            && Enum.TryParse(type.Value, out BloodType bloodType))
            bloodGroup.Type = bloodType;

        if (node.TryGet("rhesusFactor", out ValueDataNode? rhesusFactor)
            && Enum.TryParse(rhesusFactor.Value, out BloodRhesusFactor bloodRhesusFactor))
            bloodGroup.RhesusFactor = bloodRhesusFactor;

        return bloodGroup;
    }

    public void CopyTo(
        ISerializationManager serializationManager,
        BloodGroup source,
        ref BloodGroup target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null
        )
    {
        target.Type = source.Type;
        target.RhesusFactor = source.RhesusFactor;
    }
}
