using Robust.Shared.Timing;

namespace Content.Shared._White.Random;

public sealed class PredictedRandomManager
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public Xoroshiro64S GetRandom(EntityUid entity) => GetRandom(_entity.GetNetEntity(entity));

    public Xoroshiro64S GetRandom(NetEntity netEntity)
    {
        long tick = _timing.CurTick.Value;
        tick <<= 32;
        tick |= (uint) netEntity.Id;

        return GetRandom(tick);
    }

    public Xoroshiro64S GetRandom(long seed) => new(seed);

    public Xoroshiro64S GetRandom() => new();

    public int Next(EntityUid entity) => GetRandom(entity).Next();

    public int Next(NetEntity netEntity) => GetRandom(netEntity).Next();

    public int Next(long seed) => GetRandom(seed).Next();

    public int Next(Xoroshiro64S random) => random.Next();

    public int Next() => GetRandom().Next();
}
