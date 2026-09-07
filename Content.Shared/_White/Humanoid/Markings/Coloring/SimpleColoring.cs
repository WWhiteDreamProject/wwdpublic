using Content.Shared._White.Appearance.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Humanoid.Markings.Coloring;

/// <summary>
/// A coloring strategy that applies a fixed, specified <see cref="Color"/> to a marking.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class SimpleColoring : IMarkingColoringStrategy
{
    /// <summary>
    /// The fixed color to be applied to the marking.
    /// </summary>
    [DataField(required: true)]
    public Color Color = Color.White;

    /// <inheritdoc />
    public Color? GetColor(Dictionary<ProtoId<BodyColorGroupPrototype>, Color> colorGroups, List<Marking> markings)
    {
        return Color;
    }
}
