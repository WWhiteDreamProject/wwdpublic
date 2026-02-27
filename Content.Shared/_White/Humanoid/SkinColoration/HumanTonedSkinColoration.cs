using Robust.Shared.Serialization;

namespace Content.Shared._White.Humanoid.SkinColoration;

/// <summary>
/// Unary coloration strategy that returns human skin tones, with 0 being lightest and 100 being darkest
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanTonedSkinColoration : ISkinColorationStrategy
{
    [DataField]
    public Color ValidHumanSkinTone = Color.FromHsv(new (0.07f, 0.2f, 1f, 1f));

    public SkinColorationStrategyInput InputType => SkinColorationStrategyInput.Unary;

    public bool VerifySkinColor(Color color)
    {
        var colorValues = Color.ToHsv(color);

        var hue = Math.Round(colorValues.X * 360f);
        var sat = Math.Round(colorValues.Y * 100f);
        var val = Math.Round(colorValues.Z * 100f);

        // rangeOffset makes it so that this value
        // is 25 <= hue <= 45
        if (hue is < 25f or > 45f)
            return false;

        // rangeOffset makes it so that these two values
        // are 20 <= sat <= 100 and 20 <= val <= 100
        // where saturation increases to 100 and the value decreases to 20
        return !(sat < 20f) && !(val < 20f);
    }

    public Color ClosestSkinColor(Color color)
    {
        return ValidHumanSkinTone;
    }

    public Color FromUnary(float color)
    {
        // 0-100, 0 being gold/yellowish and 100 being dark
        // HSV based
        //
        // 0-20 changes the hue
        // 20-100 changes the value
        // 0 is 45 - 20 - 100
        // 20 is 25 - 20 - 100
        // 100 is 25 - 100 - 20

        var tone = Math.Clamp(color, 0f, 100f);

        var rangeOffset = tone - 20f;

        var hue = 25f;
        var sat = 20f;
        var val = 100f;

        if (rangeOffset <= 0)
        {
            // First 20 values adjust hue.
            hue += Math.Abs(rangeOffset);
        }
        else
        {
            // Remaining 80 values adjust saturation and value.
            sat += rangeOffset;
            val -= rangeOffset;
        }

        return Color.FromHsv(new (hue / 360f, sat / 100f, val / 100f, 1.0f));
    }

    public float ToUnary(Color color)
    {
        var hsv = Color.ToHsv(color);
        // check for hue/value first if hue is lower than this percentage,
        // and the value is 1.0
        // then it'll be hued
        if (Math.Clamp(hsv.X, 25f / 360f, 1) > 25f / 360f && hsv.Z == 1.0)
            return Math.Abs(45 - hsv.X * 360);

        // otherwise it'll directly be the saturation.
        return hsv.Y * 100;
    }
}
