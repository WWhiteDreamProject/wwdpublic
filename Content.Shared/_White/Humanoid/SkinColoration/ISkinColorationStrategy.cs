using Robust.Shared.Serialization;

namespace Content.Shared._White.Humanoid.SkinColoration;

/// <summary>
/// Takes in the given <see cref="SkinColorationStrategyInput" /> and returns an adjusted Color
/// </summary>
public interface ISkinColorationStrategy
{
    /// <summary>
    /// The type of input expected by the implementor; callers should consult InputType before calling the methods that require a given input
    /// </summary>
    SkinColorationStrategyInput InputType { get; }

    /// <summary>
    /// Returns whether the provided <see cref="Color" /> is within bounds of this strategy
    /// </summary>
    bool VerifySkinColor(Color color);

    /// <summary>
    /// Returns the closest skin color that this strategy would provide to the given <see cref="Color" />
    /// </summary>
    Color ClosestSkinColor(Color color);

    /// <summary>
    /// Returns the input if it passes <see cref="VerifySkinColor" />, otherwise returns <see cref="ClosestSkinColor" />
    /// </summary>
    Color EnsureVerified(Color color)
    {
        if (VerifySkinColor(color))
        {
            return color;
        }

        return ClosestSkinColor(color);
    }

    /// <summary>
    /// Returns a color representation of the given unary input
    /// </summary>
    Color FromUnary(float unary)
    {
        throw new InvalidOperationException("This coloration strategy does not support unary input");
    }

    /// <summary>
    /// Returns a color representation of the given unary input
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
        if (minHue > maxHue) // Wraps around 1.0 (e.g., reds)
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
        if (!(minHue > maxHue)) // Wraps around 1.0
            return Math.Clamp(hue, minHue, maxHue);

        // If it's already in the valid range, do nothing.
        if (hue >= minHue || hue <= maxHue)
            return hue;

        // It's in the "invalid" gap between maxHue and minHue. Find the closest boundary.
        var mid = (maxHue + minHue) / 2f;
        return hue > mid ? minHue : maxHue;
    }
}

/// <summary>
/// The type of input taken by a <see cref="ISkinColorationStrategy" />
/// </summary>
[Serializable, NetSerializable]
public enum SkinColorationStrategyInput
{
    /// <summary>
    /// A single floating point number from 0 to 100 (inclusive)
    /// </summary>
    Unary,

    /// <summary>
    /// A <see cref="Color" />
    /// </summary>
    Color,
}
