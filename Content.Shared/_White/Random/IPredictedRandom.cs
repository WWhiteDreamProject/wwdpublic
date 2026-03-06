namespace Content.Shared._White.Random;

public interface IPredictedRandom
{
    /// <summary> Get the underlying <see cref="Xoroshiro64S"/>.</summary>
    Xoroshiro64S GetRandom();

    /// <inheritdoc cref="GetRandom()"/>
    Xoroshiro64S GetRandom(EntityUid entity);

    /// <inheritdoc cref="GetRandom()"/>
    Xoroshiro64S GetRandom(NetEntity netEntity);

    /// <inheritdoc cref="GetRandom()"/>
    Xoroshiro64S GetRandom(long seed);

    /// <summary> Get random <see cref="int"/> value.</summary>
    public int Next() => GetRandom().Next();

    /// <inheritdoc cref="Next()"/>
    public int Next(EntityUid entity) => GetRandom(entity).Next();

    /// <inheritdoc cref="Next()"/>
    public int Next(NetEntity netEntity) => GetRandom(netEntity).Next();

    /// <inheritdoc cref="Next()"/>
    public int Next(long seed) => GetRandom(seed).Next();

    /// <inheritdoc cref="Next()"/>
    public int Next(Xoroshiro64S random) => random.Next();
}

