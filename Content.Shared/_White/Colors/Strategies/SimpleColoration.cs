using Robust.Shared.Serialization;

namespace Content.Shared._White.Colors.Strategies;

/// <summary>
/// Coloration strategy that accepts any color without validation or transformation.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class SimpleColoration : IColorationStrategy
{
    /// <inheritdoc />
    public ColorationStrategyInput InputType => ColorationStrategyInput.Color;

    /// <inheritdoc />
    public bool VerifyColor(Color color)
    {
        return true;
    }

    /// <inheritdoc />
    public Color ClosestColor(Color color)
    {
        return color;
    }
}
