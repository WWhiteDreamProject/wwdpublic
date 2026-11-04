using Content.Shared._White.Appearance.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Appearance;

[DataDefinition]
[Serializable, NetSerializable]
public partial record struct BodyAppearanceProviderEntry()
{
    /// <summary>
    /// The specific color group of this provider data.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<BodyColorGroupPrototype> Group = "Skin";

    /// <summary>
    /// The specific color of this provider data.
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// The specific sprite index of this provider data.
    /// </summary>
    [DataField]
    public string Index = "default";

    /// <summary>
    /// The specific sprite path of this provider data.
    /// </summary>
    [DataField(required: true)]
    public string Path = "";

    /// <summary>
    /// The specific sprite state of this provider data.
    /// </summary>
    [DataField(required: true)]
    public string State = "";

    /// <summary>
    /// Returns a new entry with the new color.
    /// </summary>
    /// <param name="color">The color to use for the entry.</param>
    /// <returns>A new entry with the specified color.</returns>
    public BodyAppearanceProviderEntry WithColor(Color color)
    {
        return this with { Color = color };
    }

    /// <summary>
    /// Returns a new entry with the new group.
    /// </summary>
    /// <param name="group">The group to use for the entry.</param>
    /// <returns>A new entry with the specified group.</returns>
    public BodyAppearanceProviderEntry WithGroup(ProtoId<BodyColorGroupPrototype> group)
    {
        return this with { Group = group };
    }

    /// <summary>
    /// Returns a new entry with the new index.
    /// </summary>
    /// <param name="index">The index to use for the entry.</param>
    /// <returns>A new entry with the specified index.</returns>
    public BodyAppearanceProviderEntry WithIndex(string index)
    {
        return this with { Index = index };
    }

    /// <summary>
    /// Returns a new entry with the new path.
    /// </summary>
    /// <param name="path">The path to use for the entry.</param>
    /// <returns>A new entry with the specified path.</returns>
    public BodyAppearanceProviderEntry WithPath(string path)
    {
        return this with { Path = path };
    }

    /// <summary>
    /// Returns a new entry with the new state.
    /// </summary>
    /// <param name="state">The state to use for the entry.</param>
    /// <returns>A new entry with the specified state.</returns>
    public BodyAppearanceProviderEntry WithState(string state)
    {
        return this with { State = state };
    }
}
