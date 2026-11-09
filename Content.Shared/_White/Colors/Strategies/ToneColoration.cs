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
    public Color ValidTone = Color.FromHex("#FFEACC");

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
            var hueRange = Hue.Item2 - Hue.Item1;
            var normalizedOffset = Math.Abs(rangeOffset) / Pivot;
            hue += hueRange * normalizedOffset;
        }
        else
        {
            var normalizedOffset = rangeOffset / (1f - Pivot);
            var satRange = Saturation.Item2 - Saturation.Item1;
            var valRange = Value.Item2 - Value.Item1;

            sat += satRange * normalizedOffset;
            val -= valRange * normalizedOffset;
        }

        return Color.FromHsv(new(hue, sat, val, 1.0f));
    }

    /// <inheritdoc />
    public float ToUnary(Color color)
    {
        var hsv = Color.ToHsv(color);

        var hueRange = Hue.Item2 - Hue.Item1;
        var satRange = Saturation.Item2 - Saturation.Item1;
        var valRange = Value.Item2 - Value.Item1;

        var hueProgress = hueRange > 0 ? (hsv.X - Hue.Item1) / hueRange : 0f;
        var satProgress = satRange > 0 ? (hsv.Y - Saturation.Item1) / satRange : 0f;
        var valProgress = valRange > 0 ? (Value.Item2 - hsv.Z) / valRange : 0f;

        if (Math.Abs(hsv.Y - Saturation.Item1) < IColorationStrategy.Epsilon &&
            Math.Abs(hsv.Z - Value.Item2) < IColorationStrategy.Epsilon)
        {
            var hueOffset = Math.Clamp(hueProgress, 0f, 1f);
            return (Pivot - hueOffset * Pivot) * 100f;
        }

        var avgProgress = (satProgress + valProgress) / 2f;
        var satValOffset = Math.Clamp(avgProgress, 0f, 1f);
        return (Pivot + satValOffset * (1f - Pivot)) * 100f;
    }
}
