using System.Linq;
using Content.Shared._White.Body.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared._White.Body.Prototypes;

[TypeSerializer]
public sealed class BodyPrototypeSerializer : ITypeReader<BodyPrototype, MappingDataNode>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null
        )
    {
        var nodes = new List<ValidationNode>();
        var prototypes = dependencies.Resolve<IPrototypeManager>();

        if (node.TryGet("bodyParts", out MappingDataNode? bodyPartsNode) && !bodyPartsNode.IsEmpty)
        {
            if (!node.TryGet("root", out ValueDataNode? root))
            {
                nodes.Add(new ErrorNode(node, $"No root value data node found"));
                return new ValidatedSequenceNode(nodes);
            }

            if (!bodyPartsNode.TryGet(root.Value, out MappingDataNode? _))
            {
                nodes.Add(new ErrorNode(bodyPartsNode, $"No slot found with id {root.Value}"));
                return new ValidatedSequenceNode(nodes);
            }

            foreach (var (_, bodyPartValue) in bodyPartsNode)
            {
                if (bodyPartValue is not MappingDataNode bodyPartNode)
                {
                    nodes.Add(new ErrorNode(bodyPartValue, $"Slot is not a mapping data node"));
                    continue;
                }

                if (bodyPartNode.TryGet<ValueDataNode>("type", out var bodyPartTypeNode)
                    && !Enum.TryParse(bodyPartTypeNode.Value, out BodyPartType _))
                    nodes.Add(new ErrorNode(bodyPartValue, $"Can't parse {bodyPartTypeNode.Value} to {typeof(BodyPartType)}"));

                if (bodyPartNode.TryGet("connections", out SequenceDataNode? connectionsNode))
                {
                    foreach (var connectionValue in connectionsNode)
                    {
                        if (connectionValue is not ValueDataNode connectionNode)
                        {
                            nodes.Add(new ErrorNode(connectionValue, $"Connection is not a value data node"));
                            continue;
                        }

                        if (!bodyPartsNode.TryGet(connectionNode.Value, out MappingDataNode? _))
                            nodes.Add(new ErrorNode(connectionValue, $"No slot found with id {connectionNode.Value}"));
                    }
                }

                if (bodyPartNode.TryGet("bones", out MappingDataNode? bonesNode))
                {
                    foreach (var (_, boneValue) in bonesNode)
                    {
                        if (boneValue is not MappingDataNode bone)
                        {
                            nodes.Add(new ErrorNode(boneValue, $"Value is not a value data node"));
                            continue;
                        }

                        if (bone.TryGet("organs", out MappingDataNode? boneOrgansNode))
                            nodes.Add(ValidateOrgan(boneOrgansNode, prototypes));

                        if (bone.TryGet("startingBone", out ValueDataNode? startingBone)
                            && !prototypes.HasIndex(startingBone.Value))
                            nodes.Add(new ErrorNode(boneValue, $"No bone entity prototype found with id {startingBone.Value}"));
                    }
                }

                if (bodyPartNode.TryGet("organs", out MappingDataNode? bodyPartOrgansNode))
                    nodes.Add(ValidateOrgan(bodyPartOrgansNode, prototypes));

                if (bodyPartNode.TryGet("startingBodyPart", out ValueDataNode? startingBodyPart)
                    && !prototypes.HasIndex(startingBodyPart.Value))
                    nodes.Add(new ErrorNode(bodyPartValue, $"No bone entity prototype found with id {startingBodyPart.Value}"));
            }
        }

        if (node.TryGet("organs", out MappingDataNode? organsNode))
            nodes.Add(ValidateOrgan(organsNode, prototypes));

        return new ValidatedSequenceNode(nodes);
    }

    private static ValidationNode ValidateOrgan(MappingDataNode organsNode, IPrototypeManager prototypes)
    {
        var nodes = new List<ValidationNode>();

        foreach (var (_, organValue) in organsNode)
        {
            if (organValue is not MappingDataNode organNode)
            {
                nodes.Add(new ErrorNode(organValue, $"Value is not a value data node"));
                continue;
            }

            if (organNode.TryGet<ValueDataNode>("type", out var organTypeNode) && !Enum.TryParse(organTypeNode.Value, out OrganType _))
                nodes.Add(new ErrorNode(organValue, $"Can't parse {organTypeNode.Value} to {typeof(OrganType)}"));

            if (organNode.TryGet("startingOrgan", out ValueDataNode? startingOrgan) && !prototypes.HasIndex(startingOrgan.Value))
                nodes.Add(new ErrorNode(organValue, $"No organ entity prototype found with id {startingOrgan.Value}"));
        }

        var validation = new ValidatedSequenceNode(nodes);
        return validation;
    }

    public BodyPrototype Read(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<BodyPrototype>? instanceProvider = null
        )
    {
        var id = node.Get<ValueDataNode>("id").Value;
        var root = string.Empty;
        var bodyParts = new Dictionary<string, BodyPartSlot>();
        var organs = new Dictionary<string, OrganSlot>();

        if (node.TryGet("bodyParts", out MappingDataNode? bodyPartsNode))
        {
            root = node.Get<ValueDataNode>("root").Value;
            foreach (var (bodyPartId, bodyPartValue) in bodyPartsNode)
            {
                var bodyPartNode = (MappingDataNode) bodyPartValue;

                var bodyPartType = BodyPartType.None;
                if (bodyPartNode.TryGet<ValueDataNode>("type", out var bodyPartTypeNode))
                    Enum.TryParse(bodyPartTypeNode.Value, out bodyPartType);

                string? startingBodyPart = null;
                if (bodyPartNode.TryGet<ValueDataNode>("startingBodyPart", out var value))
                    startingBodyPart = value.Value;

                HashSet<string> connections = new();
                if (bodyPartNode.TryGet("connections", out SequenceDataNode? connectionsNode))
                {
                    foreach (var connection in connectionsNode.Cast<ValueDataNode>())
                        connections.Add(connection.Value);
                }

                Dictionary<string, BoneSlot> bones = new();
                if (bodyPartNode.TryGet("bones", out MappingDataNode? bonesNode))
                {
                    foreach (var (boneId, boneValue) in bonesNode)
                    {
                        var boneNode = (MappingDataNode) boneValue;

                        var boneType = BoneType.None;
                        if (boneNode.TryGet<ValueDataNode>("type", out var organTypeNode))
                            Enum.TryParse(organTypeNode.Value, out boneType);

                        var boneOrgans = new Dictionary<string, OrganSlot>();
                        if (boneNode.TryGet("organs", out MappingDataNode? boneOrgansNode))
                            boneOrgans = ReadOrgan(boneOrgansNode);

                        string? startingBone = null;
                        if (boneNode.TryGet<ValueDataNode>("startingBone", out var startingBoneNode))
                            startingBone = startingBoneNode.Value;

                        bones.Add(boneId, new BoneSlot(boneType, boneOrgans, startingBone));
                    }
                }

                Dictionary<string, OrganSlot> bodyPartOrgans = new();
                if (bodyPartNode.TryGet("organs", out MappingDataNode? bodyPartOrgansNode))
                    bodyPartOrgans = ReadOrgan(bodyPartOrgansNode);

                var bodyPart = new BodyPartSlot(bodyPartType, connections, bones, bodyPartOrgans, startingBodyPart);
                bodyParts.Add(bodyPartId, bodyPart);
            }

            foreach (var (bodyPartsSlotId, bodyPartsSlot) in bodyParts)
            {
                foreach (var connection in bodyPartsSlot.Connections.ToList())
                    bodyParts[connection].Connections.Add(bodyPartsSlotId);
            }
        }

        if (node.TryGet("organs", out MappingDataNode? organsNode))
            organs = ReadOrgan(organsNode);

        return new BodyPrototype(id, root, bodyParts, organs);
    }

    private Dictionary<string, OrganSlot> ReadOrgan(MappingDataNode organsNode)
    {
        var organs = new Dictionary<string, OrganSlot>();

        foreach (var (organKey, organValueNode) in organsNode)
        {
            var organNode = (MappingDataNode) organValueNode;

            var organType = OrganType.None;
            if (organNode.TryGet<ValueDataNode>("type", out var organTypeNode))
                Enum.TryParse(organTypeNode.Value, out organType);

            string? startingOrgan = null;
            if (organNode.TryGet<ValueDataNode>("startingOrgan", out var startingOrganNode))
                startingOrgan = startingOrganNode.Value;

            organs.Add(organKey, new OrganSlot(organType, startingOrgan));
        }

        return organs;
    }
}
