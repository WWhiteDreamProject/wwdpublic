using Content.Shared._White.Appearance.Prototypes;
using Content.Shared._White.Colors.Strategies;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Humanoid.Markings.Coloring;

/// <summary>
/// A coloring strategy that derives a marking's color from a specific <see cref="BodyColorGroupPrototype"/>.
/// Optionally applies an <see cref="IColorationStrategy"/> to post-process the retrieved base color.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class BodyColorGroupColoring : IMarkingColoringStrategy
{
    /// <summary>
    /// The target body color group used as the base for this calculation.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<BodyColorGroupPrototype> Group;

    /// <summary>
    /// An optional post-processing strategy applied to the group's color.
    /// </summary>
    [DataField]
    public IColorationStrategy? Strategy;

    /// <inheritdoc />
    public Color? GetColor(Dictionary<ProtoId<BodyColorGroupPrototype>, Color> colorGroups, List<Marking> markings)
    {
        if (!colorGroups.TryGetValue(Group, out var color))
            return null;

        return Strategy?.EnsureVerified(color) ?? color;
    }
}
