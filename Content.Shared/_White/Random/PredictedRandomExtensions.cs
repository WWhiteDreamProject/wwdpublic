using Robust.Shared.Utility;

namespace Content.Shared._White.Random;

public static class PredictedRandomExtensions
{
    private static readonly double DoubleFactor = 1.0 / int.MaxValue;
    private static readonly float FloatFactor = (float) DoubleFactor;

    /// <inheritdoc cref="Next(IPredictedRandom, int, int)"/>
    public static int Next(this IPredictedRandom random, EntityUid entity, int minValue, int maxValue)
    {
        return MinMax(random.Next(entity), maxValue, minValue);
    }

    /// <inheritdoc cref="Next(IPredictedRandom, int, int)"/>
    public static int Next(this IPredictedRandom random, NetEntity netEntity, int minValue, int maxValue)
    {
        return MinMax(random.Next(netEntity), maxValue, minValue);
    }

    /// <inheritdoc cref="Next(IPredictedRandom, int, int)"/>
    public static int Next(this IPredictedRandom random, long seed, int minValue, int maxValue)
    {
        return MinMax(random.Next(seed), maxValue, minValue);
    }

    /// <inheritdoc cref="Next(IPredictedRandom, int, int)"/>
    public static int Next(this Xoroshiro64S random, int minValue, int maxValue)
    {
        return MinMax(random.Next(), maxValue, minValue);
    }

    /// <summary>Get random <see cref="int"/> value in range of <paramref name="minValue"/> (included) and <paramref name="maxValue"/> (excluded). </summary>
    /// <param name="random">The <see cref="IPredictedRandom">random</see> instance to run on.</param>
    /// <param name="minValue">Random value should be greater or equal to this value.</param>
    /// <param name="maxValue">Random value should be less than this value.</param>
    public static int Next(this IPredictedRandom random, int minValue, int maxValue)
    {
        return MinMax(random.Next(), maxValue, minValue);
    }

    /// <inheritdoc cref="NextAngle(IPredictedRandom)"/>
    public static Angle NextAngle(this IPredictedRandom random, EntityUid entity)
    {
        return NextFloat(random, entity) * MathF.Tau;
    }

    /// <inheritdoc cref="NextAngle(IPredictedRandom)"/>
    public static Angle NextAngle(this IPredictedRandom random, NetEntity netEntity)
    {
        return NextFloat(random, netEntity) * MathF.Tau;
    }

    /// <inheritdoc cref="NextAngle(IPredictedRandom)"/>
    public static Angle NextAngle(this IPredictedRandom random, long seed)
    {
        return NextFloat(random, seed) * MathF.Tau;
    }

    /// <inheritdoc cref="NextAngle(IPredictedRandom)"/>
    public static Angle NextAngle(this Xoroshiro64S random)
    {
        return NextFloat(random) * MathF.Tau;
    }

    /// <summary> Get random <see cref="Angle"/> value in range of 0 (included) and <see cref="MathF.Tau"/> (excluded). </summary>
    public static Angle NextAngle(this IPredictedRandom random)
    {
        return NextFloat(random) * MathF.Tau;
    }

    /// <inheritdoc cref="NextDouble(IPredictedRandom)"/>
    public static double NextDouble(this IPredictedRandom random, EntityUid entity)
    {
        return random.Next(entity) * DoubleFactor;
    }

    /// <inheritdoc cref="NextDouble(IPredictedRandom)"/>
    public static double NextDouble(this IPredictedRandom random, NetEntity netEntity)
    {
        return random.Next(netEntity) * DoubleFactor;
    }

    /// <inheritdoc cref="NextDouble(IPredictedRandom)"/>
    public static double NextDouble(this IPredictedRandom random, long seed)
    {
        return random.Next(seed) * DoubleFactor;
    }

    /// <inheritdoc cref="NextDouble(IPredictedRandom)"/>
    public static double NextDouble(this Xoroshiro64S random)
    {
        return random.Next() * DoubleFactor;
    }

    /// <summary> Get random <see cref="double"/> value between 0 (included) and 1 (excluded). </summary>
    public static double NextDouble(this IPredictedRandom random)
    {
        return random.Next() * DoubleFactor;
    }

    /// <inheritdoc cref="NextFloat(IPredictedRandom)"/>
    public static float NextFloat(this IPredictedRandom random, EntityUid entity)
    {
        return random.Next(entity) * FloatFactor;
    }

    /// <inheritdoc cref="NextFloat(IPredictedRandom)"/>
    public static float NextFloat(this IPredictedRandom random, NetEntity netEntity)
    {
        return random.Next(netEntity) * FloatFactor;
    }

    /// <inheritdoc cref="NextFloat(IPredictedRandom)"/>
    public static float NextFloat(this IPredictedRandom random, long seed)
    {
        return random.Next(seed) * FloatFactor;
    }

    /// <inheritdoc cref="NextFloat(IPredictedRandom)"/>
    public static float NextFloat(this Xoroshiro64S random)
    {
        return random.Next() * FloatFactor;
    }

    /// <summary> Get random <see cref="float"/> value between 0 (included) and 1 (excluded). </summary>
    public static float NextFloat(this IPredictedRandom random)
    {
        return random.Next() * FloatFactor;
    }

    /// <inheritdoc cref="NextFloat(IPredictedRandom, float)"/>
    public static float NextFloat(this IPredictedRandom random, EntityUid entity, float maxValue)
    {
        return random.NextFloat() * maxValue;
    }

    /// <inheritdoc cref="NextFloat(IPredictedRandom, float)"/>
    public static float NextFloat(this IPredictedRandom random, NetEntity netEntity, float maxValue)
    {
        return random.NextFloat() * maxValue;
    }

    /// <inheritdoc cref="NextFloat(IPredictedRandom, float)"/>
    public static float NextFloat(this IPredictedRandom random, long seed, float maxValue)
    {
        return random.NextFloat() * maxValue;
    }

    /// <inheritdoc cref="NextFloat(IPredictedRandom, float)"/>
    public static float NextFloat(this Xoroshiro64S random, float maxValue)
    {
        return random.NextFloat() * maxValue;
    }

    /// <summary> Get random <see cref="float"/> value in range of 0 (included) and <paramref name="maxValue"/> (excluded). </summary>
    /// <param name="random">The <see cref="IPredictedRandom">random</see> instance to run on.</param>
    /// <param name="maxValue">Random value should be less than this value.</param>
    public static float NextFloat(this IPredictedRandom random, float maxValue)
    {
        return random.NextFloat() * maxValue;
    }

    /// <inheritdoc cref="NextFloat(IPredictedRandom, float, float)"/>
    public static float NextFloat(this IPredictedRandom random, EntityUid entity, float minValue, float maxValue)
    {
        return MinMax(random.NextFloat(entity), minValue, maxValue);
    }

    /// <inheritdoc cref="NextFloat(IPredictedRandom, float, float)"/>
    public static float NextFloat(this IPredictedRandom random, NetEntity netEntity, float minValue, float maxValue)
    {
        return MinMax(random.NextFloat(netEntity), minValue, maxValue);
    }

    /// <inheritdoc cref="NextFloat(IPredictedRandom, float, float)"/>
    public static float NextFloat(this IPredictedRandom random, long seed, float minValue, float maxValue)
    {
        return MinMax(random.NextFloat(seed), minValue, maxValue);
    }

    /// <inheritdoc cref="NextFloat(IPredictedRandom, float, float)"/>
    public static float NextFloat(this Xoroshiro64S random, float minValue, float maxValue)
    {
        return MinMax(random.NextFloat(), minValue, maxValue);
    }

    /// <summary>Get random <see cref="float"/> value in range of <paramref name="minValue"/> (included) and <paramref name="maxValue"/> (excluded). </summary>
    /// <param name="random">The <see cref="IPredictedRandom">random</see> instance to run on.</param>
    /// <param name="minValue">Random value should be greater or equal to this value.</param>
    /// <param name="maxValue">Random value should be less than this value.</param>
    public static float NextFloat(this IPredictedRandom random, float minValue, float maxValue)
    {
        return MinMax(random.NextFloat(), minValue, maxValue);
    }

    /// <summary>Picks a random element from a collection.</summary>
    public static T Pick<T>(this IPredictedRandom random, IReadOnlyList<T> list)
    {
        var index = random.Next(list.Count);
        return list[index];
    }

    /// <inheritdoc cref="Prob(IPredictedRandom, float)"/>
    public static bool Prob(this IPredictedRandom random, EntityUid entity, float chance)
    {
        return Prob(random.NextDouble(entity), chance);
    }

    /// <inheritdoc cref="Prob(IPredictedRandom, float)"/>
    public static bool Prob(this IPredictedRandom random, NetEntity netEntity, float chance)
    {
        return Prob(random.NextDouble(netEntity), chance);
    }

    /// <inheritdoc cref="Prob(IPredictedRandom, float)"/>
    public static bool Prob(this IPredictedRandom random, long seed, float chance)
    {
        return Prob(random.NextDouble(seed), chance);
    }

    /// <inheritdoc cref="Prob(IPredictedRandom, float)"/>
    public static bool Prob(this Xoroshiro64S random, float chance)
    {
        return Prob(random.NextDouble(), chance);
    }

    /// <summary> Have a certain chance to return a boolean.</summary>
    /// <param name="random">The <see cref="IPredictedRandom">random</see> instance to run on.</param>
    /// <param name="chance">The chance to pass, from 0 to 1.</param>
    public static bool Prob(this IPredictedRandom random, float chance)
    {
        return Prob(random.NextDouble(), chance);
    }

    private static bool Prob(double value, float chance)
    {
        DebugTools.Assert(chance is <= 1 and >= 0, $"Chance must be in the range 0-1. It was {chance}.");

        return value < chance;
    }

    private static int MinMax(int value, int minValue, int maxValue)
    {
        return value * (maxValue - minValue) + minValue;
    }

    private static float MinMax(float value, float minValue, float maxValue)
    {
        return value * (maxValue - minValue) + minValue;
    }
}
