using Robust.Shared.Serialization;

namespace Content.Shared._White.Humanoid.Coloration;

/// <summary>
/// Coloration strategy that clamps the color within the HSL colorspace.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ClampedHslColoration : IColorationStrategy
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
    /// The (min, max) of the lightness channel.
    /// </summary>
    [DataField]
    public (float, float)? Lightness;

    public ColorationStrategyInput InputType => ColorationStrategyInput.Color;

    public bool VerifyColor(Color color)
    {
        var hsl = Color.ToHsl(color);

        if (Hue is var (minHue, maxHue) && !IColorationStrategy.IsHueInRange(hsl.X, minHue, maxHue))
            return false;

        if (Saturation is var (minSat, maxSat) && (hsl.Y < minSat - IColorationStrategy.Epsilon || hsl.Y > maxSat + IColorationStrategy.Epsilon))
            return false;

        return Lightness is not var (minLight, maxLight) || (!(hsl.Z < minLight - IColorationStrategy.Epsilon) && !(hsl.Z > maxLight + IColorationStrategy.Epsilon));
    }

    public Color ClosestColor(Color color)
    {
        var hsl = Color.ToHsl(color);

        if (Hue is var (minHue, maxHue))
            hsl.X = IColorationStrategy.ClampHue(hsl.X, minHue, maxHue);

        if (Saturation is var (minSat, maxSat))
            hsl.Y = Math.Clamp(hsl.Y, minSat, maxSat);

        if (Lightness is var (minLight, maxLight))
            hsl.Z = Math.Clamp(hsl.Z, minLight, maxLight);

        return Color.FromHsl(hsl);
    }
}
