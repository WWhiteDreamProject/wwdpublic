using Robust.Shared.Serialization;

namespace Content.Shared._White.Humanoid.Coloration;

/// <summary>
/// Takes in the given <see cref="ColorationStrategyInput" /> and returns an adjusted color.
/// </summary>
public interface IColorationStrategy
{
    /// <summary>
    /// The type of input expected by the implementor; callers should consult InputType before calling the methods that require a given input.
    /// </summary>
    ColorationStrategyInput InputType { get; }

    /// <summary>
    /// Returns whether the provided <see cref="Color" /> is within bounds of this strategy.
    /// </summary>
    bool VerifyColor(Color color);

    /// <summary>
    /// Returns the closest  color that this strategy would provide to the given <see cref="Color" />.
    /// </summary>
    Color ClosestColor(Color color);

    /// <summary>
    /// Returns the input if it passes <see cref="VerifyColor" />, otherwise returns <see cref="ClosestColor" />.
    /// </summary>
    Color EnsureVerified(Color color)
    {
        if (VerifyColor(color))
        {
            return color;
        }

        return ClosestColor(color);
    }

    /// <summary>
    /// Returns a color representation of the given unary input.
    /// </summary>
    Color FromUnary(float unary)
    {
        throw new InvalidOperationException("This coloration strategy does not support unary input");
    }

    /// <summary>
    /// Returns a color representation of the given unary input.
    /// </summary>
    float ToUnary(Color color)
    {
        throw new InvalidOperationException("This coloration strategy does not support unary input");
    }

    /// <summary>
    /// An empirically determined epsilon to account for floating-point drift during RGB -> HSL/HSV -> RGB conversions.
    /// Based on high-iteration testing (50M+ samples) which showed a max drift of ~4.9E-6 for HSL.
    /// A value of 1E-5f provides a robust safety margin.
    /// </summary>
    public const float Epsilon = 1e-5f; // 0.00001

    /// <summary>
    /// Checks if a hue value is within a specified range, correctly handling ranges that wrap around 1.0 (e.g., reds).
    /// </summary>
    /// <param name="hue">The hue value to check (0.0 to 1.0).</param>
    /// <param name="minHue">The minimum bound of the hue range.</param>
    /// <param name="maxHue">The maximum bound of the hue range.</param>
    /// <returns>True if the hue is within the range; otherwise, false.</returns>
    public static bool IsHueInRange(float hue, float minHue, float maxHue)
    {
        if (minHue > maxHue)
            return hue >= minHue - Epsilon || hue <= maxHue + Epsilon;

        return hue >= minHue - Epsilon && hue <= maxHue + Epsilon;
    }

    /// <summary>
    /// Clamps a hue value to the closest boundary of a given range, correctly handling ranges that wrap around 1.0.
    /// </summary>
    /// <param name="hue">The hue value to clamp (0.0 to 1.0).</param>
    /// <param name="minHue">The minimum bound of the hue range.</param>
    /// <param name="maxHue">The maximum bound of the hue range.</param>
    /// <returns>The clamped hue value, adjusted to the nearest boundary if it was outside the valid range.</returns>
    public static float ClampHue(float hue, float minHue, float maxHue)
    {
        if (minHue <= maxHue)
            return Math.Clamp(hue, minHue, maxHue);

        if (hue >= minHue || hue <= maxHue)
            return hue;

        var mid = (maxHue + minHue) / 2f;
        return hue > mid ? minHue : maxHue;
    }
}

/// <summary>
/// The type of input taken by a <see cref="IColorationStrategy"/>.
/// </summary>
[Serializable, NetSerializable]
public enum ColorationStrategyInput
{
    /// <summary>
    /// A single floating point number from 0 to 100 (inclusive).
    /// </summary>
    Unary,

    /// <summary>
    /// A <see cref="Color" />.
    /// </summary>
    Color,
}
