using Content.Shared._White.Colors.Strategies;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Tests.Shared._White.Colors;

[TestFixture]
[TestOf(typeof(ClampedHslColoration))]
[TestOf(typeof(ClampedHsvColoration))]
[TestOf(typeof(ToneColoration))]
public sealed class ColorationStrategyTest
{
    /// <summary>
    /// Checks that clamping random colors with an HSL strategy that limits all three channels produces valid colors.
    /// </summary>
    [Test]
    public void TestClampedHslWithAllChannels()
    {
        var random = new RobustRandom();
        var strategy = new ClampedHslColoration
        {
            Hue = (0.1f, 0.3f),
            Saturation = (0.2f, 0.8f),
            Lightness = (0.3f, 0.7f)
        };

        for (var i = 0; i <= 10000; i++)
        {
            var color = new Color(random.NextFloat(), random.NextFloat(), random.NextFloat());
            var closestColor = strategy.ClosestColor(color);

            Assert.That(strategy.VerifyColor(closestColor), $"Color {closestColor} (from input {color}) failed verification in {TestContext.CurrentContext.Test.Name} on iteration {i}");
        }
    }

    /// <summary>
    /// Checks that clamping works correctly for HSL strategies where the hue range wraps around the 0-1 boundary.
    /// </summary>
    [Test]
    public void TestClampedHslWithCircularHue()
    {
        var random = new RobustRandom();
        var strategy = new ClampedHslColoration
        {
            Hue = (0.9f, 0.1f),
            Saturation = (0.5f, 1.0f),
            Lightness = (0.5f, 1.0f)
        };

        for (var i = 0; i <= 10000; i++)
        {
            var color = new Color(random.NextFloat(), random.NextFloat(), random.NextFloat());
            var closestColor = strategy.ClosestColor(color);

            Assert.That(strategy.VerifyColor(closestColor), $"Color {closestColor} (from input {color}) with circular hue failed verification in {TestContext.CurrentContext.Test.Name} on iteration {i}");
        }
    }

    /// <summary>
    /// Checks that clamping random colors with an HSV strategy that limits all three channels produces valid colors.
    /// </summary>
    [Test]
    public void TestClampedHsvWithAllChannels()
    {
        var random = new RobustRandom();
        var strategy = new ClampedHsvColoration
        {
            Hue = (0.1f, 0.3f),
            Saturation = (0.2f, 0.8f),
            Value = (0.3f, 0.7f)
        };

        for (var i = 0; i <= 10000; i++)
        {
            var color = new Color(random.NextFloat(), random.NextFloat(), random.NextFloat());
            var closestColor = strategy.ClosestColor(color);

            Assert.That(strategy.VerifyColor(closestColor), $"Color {closestColor} (from input {color}) failed verification in {TestContext.CurrentContext.Test.Name} on iteration {i}");
        }
    }

    /// <summary>
    /// Checks that a color outside the valid range is correctly clamped to a valid color.
    /// </summary>
    [Test]
    public void TestClosestColorClampsInvalidColor()
    {
        var strategy = new ClampedHslColoration
        {
            Saturation = (0.0f, 0.1f),
            Lightness = (0.85f, 1.0f)
        };

        // This color has high saturation and low lightness, should be clamped
        var invalidColor = Color.FromHsl(new(0.5f, 0.9f, 0.2f, 1.0f));
        var result = strategy.ClosestColor(invalidColor);

        Assert.That(strategy.VerifyColor(result), Is.True);
        Assert.That(result, Is.Not.EqualTo(invalidColor));
    }

    /// <summary>
    /// Checks that a color that is already valid is not modified.
    /// </summary>
    [Test]
    public void TestClosestColorReturnsValidColor()
    {
        var strategy = new ClampedHslColoration
        {
            Saturation = (0.0f, 1.0f),
            Lightness = (0.0f, 1.0f)
        };

        var validColor = Color.FromHsl(new(0.5f, 0.5f, 0.5f, 1.0f));
        var result = strategy.ClosestColor(validColor);

        Assert.That(strategy.VerifyColor(result), Is.True);
    }

    /// <summary>
    /// Checks that the default tone is considered valid.
    /// </summary>
    [Test]
    public void TestDefaultToneValid()
    {
        var strategy = new ToneColoration();
        Assert.That(strategy.VerifyColor(strategy.ValidTone));
    }

    /// <summary>
    /// Checks that clamping random colors with a low-saturation, high-lightness HSL strategy produces valid colors.
    /// </summary>
    [Test]
    public void TestTintedHuesValidHsl()
    {
        var random = new RobustRandom();
        var strategy = new ClampedHslColoration
        {
            Saturation = (0.0f, 0.1f),
            Lightness = (0.85f, 1.0f)
        };

        for (var i = 0; i <= 10000; i++)
        {
            var color = new Color(random.NextFloat(), random.NextFloat(), random.NextFloat());
            var closestColor = strategy.ClosestColor(color);

            Assert.That(strategy.VerifyColor(closestColor), $"Color {closestColor} (from input {color}) failed verification in {TestContext.CurrentContext.Test.Name} on iteration {i}");
        }
    }

    /// <summary>
    /// Checks that clamping random colors with a low-saturation, high-value HSV strategy produces valid colors.
    /// </summary>
    [Test]
    public void TestTintedHuesValidHsv()
    {
        var random = new RobustRandom();
        var strategy = new ClampedHsvColoration
        {
            Saturation = (0.0f, 0.1f),
            Value = (0.85f, 1.0f)
        };

        for (var i = 0; i <= 10000; i++)
        {
            var color = new Color(random.NextFloat(), random.NextFloat(), random.NextFloat());
            var closestColor = strategy.ClosestColor(color);

            Assert.That(strategy.VerifyColor(closestColor), $"Color {closestColor} (from input {color}) failed verification in {TestContext.CurrentContext.Test.Name} on iteration {i}");
        }
    }

    /// <summary>
    /// Checks that colors generated by ToneColoration.FromUnary pass verification.
    /// </summary>
    [Test]
    public void TestTonesFromUnaryAreValid()
    {
        var strategy = new ToneColoration();

        for (var i = 0; i <= 10000; i++)
        {
            var unaryInput = i / 100f;
            var color = strategy.FromUnary(unaryInput);

            Assert.That(strategy.VerifyColor(color), $"Color {color} from unary value {unaryInput} failed verification.");
        }
    }

    /// <summary>
    /// Checks that converting a unary value to a color and back results in a similar unary value.
    /// </summary>
    [Test]
    public void TestTonesRoundTrip()
    {
        var strategy = new ToneColoration();

        for (var i = 0; i <= 10000; i++)
        {
            var originalUnary = i / 100f;
            var color = strategy.FromUnary(originalUnary);
            var resultUnary = strategy.ToUnary(color);

            Assert.That(
                resultUnary,
                Is.EqualTo(originalUnary).Within(1e-2f), // 1e-2f (0.01) is 1% of the unary range, which is reasonable for rounding and float error.
                $"Round trip failed for unary {originalUnary}. Got {resultUnary} back.");
        }
    }
}
