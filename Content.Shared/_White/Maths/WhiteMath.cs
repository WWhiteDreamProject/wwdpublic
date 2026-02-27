namespace Content.Shared._White.Maths;

public static class WhiteMath
{
    /// <summary>
    /// Applies a frame-time-based adjustment to a value, ensuring smooth changes.
    /// Used for interpolating values.
    /// </summary>
    public static float Diff(float value, float lastFrameTime, float speed = 1f)
    {
        var adjustment = value * speed * lastFrameTime;
        return value < 0f ? Math.Clamp(adjustment, value, -value) : Math.Clamp(adjustment, -value, value);
    }

    /// <summary>
    /// Applies a logistic (S-shaped) growth curve to a value.
    /// </summary>
    public static float LogisticsGrowth(float value, float inflection, float steepness)
    {
        return 1 / (1 + MathF.Pow(float.E, -steepness * (value - inflection)));
    }
}
