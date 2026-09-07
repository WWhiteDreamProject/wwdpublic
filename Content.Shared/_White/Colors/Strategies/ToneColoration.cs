using Robust.Shared.Serialization;

namespace Content.Shared._White.Colors.Strategies;

/// <summary>
/// Unary coloration strategy that map a single 0-100 value onto a "tone curve" through HSV space.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ToneColoration : IColorationStrategy
{
    /// <summary>
    /// The color returned by <see cref="ClosestColor"/> when the input color doesn't verify.
    /// </summary>
    [DataField]
    public Color ValidTone;

    /// <summary>
    /// The unary value (0.0-1.0) at which the curve switches from varying hue to varying saturation/value.
    /// </summary>
    [DataField]
    public float Pivot = 0.2f;

    /// <summary>
    /// Defines the valid (min, max) range for the hue channel (0.0-1.0).
    /// </summary>
    [DataField]
    public (float, float) Hue = (0.07f, 0.125f);

    /// <summary>
    /// The (min, max) of the saturation channel.
    /// </summary>
    [DataField]
    public (float, float) Saturation = (0.2f, 1f);

    /// <summary>
    /// The (min, max) of the value channel.
    /// </summary>
    [DataField]
    public (float, float) Value = (0.2f, 1f);

    /// <inheritdoc />
    public ColorationStrategyInput InputType => ColorationStrategyInput.Unary;

    /// <inheritdoc />
    public bool VerifyColor(Color color)
    {
        var hsv = Color.ToHsv(color);

        if (!IColorationStrategy.IsHueInRange(hsv.X, Hue.Item1, Hue.Item2))
            return false;

        if (hsv.Y < Saturation.Item1 - IColorationStrategy.Epsilon || hsv.Y > Saturation.Item2 + IColorationStrategy.Epsilon)
            return false;

        return !(hsv.Z < Value.Item1 - IColorationStrategy.Epsilon) && !(hsv.Z > Value.Item2 + IColorationStrategy.Epsilon);
    }

    /// <inheritdoc />
    public Color ClosestColor(Color color)
    {
        return ValidTone;
    }

    /// <inheritdoc />
    public Color FromUnary(float color)
    {
        var tone = Math.Clamp(color, 0f, 100f) / 100f;
        var rangeOffset = tone - Pivot;

        var hue = Hue.Item1;
        var sat = Saturation.Item1;
        var val = Value.Item2;

        if (rangeOffset <= 0)
        {
            hue += Math.Abs(rangeOffset) / 3.6f;
        }
        else
        {
            sat += rangeOffset;
            val -= rangeOffset;
        }

        return Color.FromHsv(new(hue, sat, val, 1.0f));
    }

    /// <inheritdoc />
    public float ToUnary(Color color)
    {
        var hsv = Color.ToHsv(color);

        if (IColorationStrategy.ClampHue(hsv.X, Hue.Item1, 1f) > Hue.Item1 && hsv.Z == 1.0)
            return Math.Abs(Hue.Item2 - hsv.X * 3.6f) * 100f;

        return hsv.Y * 100f;
    }
}
