using Content.Shared._White.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared._White.Damage;

public sealed class DamageSpecifierSerializer :
    ITypeSerializer<DamageSpecifier, MappingDataNode>,
    ITypeCopier<DamageSpecifier>
{
    public DamageSpecifier Read(
        ISerializationManager serialization,
        MappingDataNode node,
        IDependencyCollection dependency,
        SerializationHookContext hookContext,
        ISerializationContext? context,
        ISerializationManager.InstantiationDelegate<DamageSpecifier>? instantiationDelegate
        )
    {
        var specifier = new DamageSpecifier();

        foreach (var (typeNode, damageNode) in node.Children)
        {
            var type = serialization.Read<ProtoId<DamageTypePrototype>>(new ValueDataNode(typeNode), hookContext, context);
            var damage = serialization.Read<FixedPoint2>(damageNode, hookContext, context);

            specifier.Add(type, damage);
        }

        return specifier;
    }

    public DataNode Write(
        ISerializationManager serialization,
        DamageSpecifier value,
        IDependencyCollection dependency,
        bool alwaysWrite = false,
        ISerializationContext? context = null
        )
    {
        var mappingNode = new MappingDataNode();

        foreach (var (type, damage) in value)
        {
            mappingNode.Add(type, serialization.WriteValue(damage, alwaysWrite, context));
        }

        return mappingNode;
    }

    public ValidationNode Validate(
        ISerializationManager serialization,
        MappingDataNode node,
        IDependencyCollection dependency,
        ISerializationContext? context = null
    )
    {
        var mapping = new Dictionary<ValidationNode, ValidationNode>();

        foreach (var (typeNode, damageNode) in node.Children)
        {
            var type = serialization.ValidateNode<ProtoId<DamageTypePrototype>>(node.GetKeyNode(typeNode), context);
            var damage = serialization.ValidateNode<FixedPoint2>(damageNode, context);

            mapping.Add(type, damage);
        }

        return new ValidatedMappingNode(mapping);
    }

    public void CopyTo(
        ISerializationManager serialization,
        DamageSpecifier source,
        ref DamageSpecifier target,
        IDependencyCollection dependency,
        SerializationHookContext hookContext,
        ISerializationContext? context = null
    )
    {
        target.Clear();

        foreach (var (type, damage) in source)
        {
            var typeCopy = serialization.CreateCopy(type, hookContext, context);
            var damageCopy = serialization.CreateCopy(damage, hookContext, context);

            target.Add(typeCopy, damageCopy);
        }
    }
}
