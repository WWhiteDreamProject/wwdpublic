using Robust.Shared.Serialization;

namespace Content.Shared._White.Humanoid.Coloration;

/// <summary>
/// Coloration strategy that clamps the color within the HSV colorspace.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ClampedHsvColoration : IColorationStrategy
{
    /// <summary>
    /// Defines the valid (min, max) range for the hue channel (0.0 to 1.0).
    /// If min > max, the range wraps around 1.0 (e.g., for reds).
    /// </summary>
    [DataField]
    public (float, float)? Hue;

    /// <summary>
    /// The (min, max) of the saturation channel.
    /// </summary>
    [DataField]
    public (float, float)? Saturation;

    /// <summary>
    /// The (min, max) of the value channel.
    /// </summary>
    [DataField]
    public (float, float)? Value;

    public ColorationStrategyInput InputType => ColorationStrategyInput.Color;

    public bool VerifyColor(Color color)
    {
        var hsv = Color.ToHsv(color);

        if (Hue is var (minHue, maxHue) && !IColorationStrategy.IsHueInRange(hsv.X, minHue, maxHue))
            return false;

        if (Saturation is var (minSat, maxSat) && (hsv.Y < minSat - IColorationStrategy.Epsilon || hsv.Y > maxSat + IColorationStrategy.Epsilon))
            return false;

        return Value is not var (minVal, maxVal) || !(hsv.Z < minVal - IColorationStrategy.Epsilon) && !(hsv.Z > maxVal + IColorationStrategy.Epsilon);
    }

    public Color ClosestColor(Color color)
    {
        var hsv = Color.ToHsv(color);

        if (Hue is var (minHue, maxHue))
            hsv.X = IColorationStrategy.ClampHue(hsv.X, minHue, maxHue);

        if (Saturation is var (minSat, maxSat))
            hsv.Y = Math.Clamp(hsv.Y, minSat, maxSat);

        if (Value is var (minVal, maxVal))
            hsv.Z = Math.Clamp(hsv.Z, minVal, maxVal);

        return Color.FromHsv(hsv);
    }
}
