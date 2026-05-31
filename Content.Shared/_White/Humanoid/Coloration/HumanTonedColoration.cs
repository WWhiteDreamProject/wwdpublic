using Robust.Shared.Serialization;

namespace Content.Shared._White.Humanoid.Coloration;

/// <summary>
/// Unary coloration strategy that returns human  tones, with 0 being lightest and 100 being darkest.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanTonedColoration : IColorationStrategy
{
    [DataField]
    public Color ValidHumanTone = Color.FromHsv(new (0.07f, 0.2f, 1f, 1f));

    public ColorationStrategyInput InputType => ColorationStrategyInput.Unary;

    public bool VerifyColor(Color color)
    {
        var colorValues = Color.ToHsv(color);

        var hue = Math.Round(colorValues.X * 360f);
        var sat = Math.Round(colorValues.Y * 100f);
        var val = Math.Round(colorValues.Z * 100f);

        if (hue is < 25f or > 45f)
            return false;

        return !(sat < 20f) && !(val < 20f);
    }

    public Color ClosestColor(Color color)
    {
        return ValidHumanTone;
    }

    public Color FromUnary(float color)
    {
        var tone = Math.Clamp(color, 0f, 100f);

        var rangeOffset = tone - 20f;

        var hue = 25f;
        var sat = 20f;
        var val = 100f;

        if (rangeOffset <= 0)
        {
            hue += Math.Abs(rangeOffset);
        }
        else
        {
            sat += rangeOffset;
            val -= rangeOffset;
        }

        return Color.FromHsv(new (hue / 360f, sat / 100f, val / 100f, 1.0f));
    }

    public float ToUnary(Color color)
    {
        var hsv = Color.ToHsv(color);

        if (Math.Clamp(hsv.X, 25f / 360f, 1) > 25f / 360f && hsv.Z == 1.0)
            return Math.Abs(45 - hsv.X * 360);

        return hsv.Y * 100;
    }
}
