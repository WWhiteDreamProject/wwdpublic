using System.Collections.Generic;
using System.Linq;
using Content.Shared.Clothing.Loadouts.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared._White.Preferences;

[TypeSerializer]
public sealed class LoadoutPreferencesSerializer : ITypeReader<Dictionary<string, Loadout>, MappingDataNode>,
                                                   ITypeReader<Dictionary<string, Loadout>, SequenceDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return serializationManager.ValidateNode<Dictionary<string, Loadout>>(node, context);
    }

    public ValidationNode Validate(ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return new ValidatedSequenceNode(new List<ValidationNode>());
    }

    public Dictionary<string, Loadout> Read(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<Dictionary<string, Loadout>>? instanceProvider = null)
    {
        var result = instanceProvider?.Invoke() ?? new Dictionary<string, Loadout>();

        foreach (var (key, valueNode) in node)
        {
            var loadout = serializationManager.Read<Loadout>(valueNode, hookCtx, context, notNullableOverride: true);

            if (!string.IsNullOrEmpty(key))
                result[key] = loadout;
        }

        return result;
    }
    public Dictionary<string, Loadout> Read(ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<Dictionary<string, Loadout>>? instanceProvider = null)
    {
        var result = instanceProvider?.Invoke() ?? new Dictionary<string, Loadout>();

        foreach (var itemNode in node.Cast<MappingDataNode>())
        {
            var selected = false;
            if (itemNode.TryGet("selected", out var selectedNode))
            {
                selected = serializationManager.Read<bool>(selectedNode, context);
            }

            if (!selected)
                continue;

            if (!itemNode.TryGet("loadoutName", out var loadoutNameNode))
                continue;

            var loadoutName = serializationManager.Read<string>(loadoutNameNode, context, notNullableOverride: true);
            if (string.IsNullOrEmpty(loadoutName))
                continue;

            string? customName = null;
            string? customDescription = null;
            string? customContent = null;
            string? customColorTint = null;
            bool? customHeirloom = null;

            if (itemNode.TryGet("customName", out var customNameNode))
                customName = serializationManager.Read<string?>(customNameNode, context);

            if (itemNode.TryGet("customDescription", out var customDescNode))
                customDescription = serializationManager.Read<string?>(customDescNode, context);

            if (itemNode.TryGet("customContent", out var customContentNode))
                customContent = serializationManager.Read<string?>(customContentNode, context);

            if (itemNode.TryGet("customColorTint", out var customColorNode))
                customColorTint = serializationManager.Read<string?>(customColorNode, context);

            if (itemNode.TryGet("customHeirloom", out var customHeirloomNode))
                customHeirloom = serializationManager.Read<bool?>(customHeirloomNode, context);

            var loadout = new Loadout(
                loadoutName,
                customName,
                customDescription,
                customContent,
                customColorTint,
                customHeirloom
            );

            result[loadoutName] = loadout;
        }

        return result;
    }
}
