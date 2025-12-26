using Robust.Shared.Utility;

namespace Content.Shared._White.Random;

public static class PredictedRandomExtensions
{
    private static readonly double DoubleFactor = 1.0 / int.MaxValue;
    private static readonly float FloatFactor = (float) DoubleFactor;

    /// <inheritdoc cref="Next(PredictedRandomManager, int, int)"/>
    public static int Next(this PredictedRandomManager randomManager, EntityUid entity, int minValue, int maxValue) =>
        MinMax(randomManager.Next(entity), maxValue, minValue);

    /// <inheritdoc cref="Next(PredictedRandomManager, int, int)"/>
    public static int Next(this PredictedRandomManager randomManager, NetEntity netEntity, int minValue, int maxValue) =>
        MinMax(randomManager.Next(netEntity), maxValue, minValue);

    /// <inheritdoc cref="Next(PredictedRandomManager, int, int)"/>
    public static int Next(this PredictedRandomManager randomManager, long seed, int minValue, int maxValue) =>
        MinMax(randomManager.Next(seed), maxValue, minValue);

    /// <inheritdoc cref="Next(PredictedRandomManager, int, int)"/>
    public static int Next(this PredictedRandomManager randomManager, Xoroshiro64S random, int minValue, int maxValue) =>
        MinMax(randomManager.Next(random), maxValue, minValue);

    /// <summary>Get randomManager <see cref="int"/> value in range of <paramref name="minValue"/> (included) and <paramref name="maxValue"/> (excluded). </summary>
    /// <param name="randomManager">The <see cref="PredictedRandomManager">randomManager</see> instance to run on.</param>
    /// <param name="minValue">randomManager value should be greater or equal to this value.</param>
    /// <param name="maxValue">randomManager value should be less than this value.</param>
    public static int Next(this PredictedRandomManager randomManager, int minValue, int maxValue) =>
        MinMax(randomManager.Next(), maxValue, minValue);

    /// <inheritdoc cref="NextAngle(PredictedRandomManager)"/>
    public static Angle NextAngle(this PredictedRandomManager randomManager, EntityUid entity) =>
        NextFloat(randomManager, entity) * MathF.Tau;

    /// <inheritdoc cref="NextAngle(PredictedRandomManager)"/>
    public static Angle NextAngle(this PredictedRandomManager randomManager, NetEntity netEntity) =>
        NextFloat(randomManager, netEntity) * MathF.Tau;

    /// <inheritdoc cref="NextAngle(PredictedRandomManager)"/>
    public static Angle NextAngle(this PredictedRandomManager randomManager, long seed) =>
        NextFloat(randomManager, seed) * MathF.Tau;

    /// <inheritdoc cref="NextAngle(PredictedRandomManager)"/>
    public static Angle NextAngle(this PredictedRandomManager randomManager, Xoroshiro64S random) =>
        NextFloat(randomManager, random) * MathF.Tau;

    /// <summary> Get random <see cref="Angle"/> value in range of 0 (included) and <see cref="MathF.Tau"/> (excluded). </summary>
    public static Angle NextAngle(this PredictedRandomManager randomManager) =>
        NextFloat(randomManager) * MathF.Tau;

    /// <inheritdoc cref="NextDouble(PredictedRandomManager)"/>
    public static double NextDouble(this PredictedRandomManager randomManager, EntityUid entity) =>
        randomManager.Next(entity) * DoubleFactor;

    /// <inheritdoc cref="NextDouble(PredictedRandomManager)"/>
    public static double NextDouble(this PredictedRandomManager randomManager, NetEntity netEntity) =>
        randomManager.Next(netEntity) * DoubleFactor;

    /// <inheritdoc cref="NextDouble(PredictedRandomManager)"/>
    public static double NextDouble(this PredictedRandomManager randomManager, long seed) =>
        randomManager.Next(seed) * DoubleFactor;

    /// <inheritdoc cref="NextDouble(PredictedRandomManager)"/>
    public static double NextDouble(this PredictedRandomManager randomManager, Xoroshiro64S random) =>
        randomManager.Next(random) * DoubleFactor;

    /// <summary> Get randomManager <see cref="double"/> value between 0 (included) and 1 (excluded). </summary>
    public static double NextDouble(this PredictedRandomManager randomManager) =>
        randomManager.Next() * DoubleFactor;

    /// <inheritdoc cref="NextFloat(PredictedRandomManager)"/>
    public static float NextFloat(this PredictedRandomManager randomManager, EntityUid entity) =>
        randomManager.Next(entity) * FloatFactor;

    /// <inheritdoc cref="NextFloat(PredictedRandomManager)"/>
    public static float NextFloat(this PredictedRandomManager randomManager, NetEntity netEntity) =>
        randomManager.Next(netEntity) * FloatFactor;

    /// <inheritdoc cref="NextFloat(PredictedRandomManager)"/>
    public static float NextFloat(this PredictedRandomManager randomManager, long seed) =>
        randomManager.Next(seed) * FloatFactor;

    /// <inheritdoc cref="NextFloat(PredictedRandomManager)"/>
    public static float NextFloat(this PredictedRandomManager randomManager, Xoroshiro64S random) =>
        randomManager.Next(random) * FloatFactor;

    /// <summary> Get randomManager <see cref="float"/> value between 0 (included) and 1 (excluded). </summary>
    public static float NextFloat(this PredictedRandomManager randomManager) =>
        randomManager.Next() * FloatFactor;

    /// <inheritdoc cref="NextFloat(PredictedRandomManager, float, float)"/>
    public static float NextFloat(this PredictedRandomManager randomManager, EntityUid entity, float minValue, float maxValue) =>
        MinMax(randomManager.NextFloat(entity), maxValue, minValue);

    /// <inheritdoc cref="NextFloat(PredictedRandomManager, float, float)"/>
    public static float NextFloat(this PredictedRandomManager randomManager, NetEntity netEntity, float minValue, float maxValue) =>
        MinMax(randomManager.NextFloat(netEntity), maxValue, minValue);

    /// <inheritdoc cref="NextFloat(PredictedRandomManager, float, float)"/>
    public static float NextFloat(this PredictedRandomManager randomManager, long seed, float minValue, float maxValue) =>
        MinMax(randomManager.NextFloat(seed), maxValue, minValue);

    /// <inheritdoc cref="NextFloat(PredictedRandomManager, float, float)"/>
    public static float NextFloat(this PredictedRandomManager randomManager, Xoroshiro64S random, float minValue, float maxValue) =>
        MinMax(randomManager.NextFloat(random), maxValue, minValue);

    /// <summary>Get randomManager <see cref="float"/> value in range of <paramref name="minValue"/> (included) and <paramref name="maxValue"/> (excluded). </summary>
    /// <param name="randomManager">The <see cref="PredictedRandomManager">randomManager</see> instance to run on.</param>
    /// <param name="minValue">randomManager value should be greater or equal to this value.</param>
    /// <param name="maxValue">randomManager value should be less than this value.</param>
    public static float NextFloat(this PredictedRandomManager randomManager, float minValue, float maxValue) =>
        MinMax(randomManager.NextFloat(), maxValue, minValue);

    /// <inheritdoc cref="Prob(PredictedRandomManager, float)"/>
    public static bool Prob(this PredictedRandomManager randomManager, EntityUid entity, float chance) =>
        Prob(randomManager.NextDouble(entity), chance);

    /// <inheritdoc cref="Prob(PredictedRandomManager, float)"/>
    public static bool Prob(this PredictedRandomManager randomManager, NetEntity netEntity, float chance) =>
        Prob(randomManager.NextDouble(netEntity), chance);

    /// <inheritdoc cref="Prob(PredictedRandomManager, float)"/>
    public static bool Prob(this PredictedRandomManager randomManager, long seed, float chance) =>
        Prob(randomManager.NextDouble(seed), chance);

    /// <inheritdoc cref="Prob(PredictedRandomManager, float)"/>
    public static bool Prob(this PredictedRandomManager randomManager, Xoroshiro64S random, float chance) =>
        Prob(randomManager.NextDouble(random), chance);

    /// <summary> Have a certain chance to return a boolean.</summary>
    /// <param name="randomManager">The <see cref="PredictedRandomManager">randomManager</see> instance to run on.</param>
    /// <param name="chance">The chance to pass, from 0 to 1.</param>
    public static bool Prob(this PredictedRandomManager randomManager, float chance) =>
        Prob(randomManager.NextDouble(), chance);

    private static bool Prob(double value, float chance)
    {
        DebugTools.Assert(chance is <= 1 and >= 0, $"Chance must be in the range 0-1. It was {chance}.");

        return value < chance;
    }

    private static int MinMax(int value, int minValue, int maxValue) =>
        value * (maxValue - minValue) + minValue;

    private static float MinMax(float value, float minValue, float maxValue) =>
        value * (maxValue - minValue) + minValue;

}
