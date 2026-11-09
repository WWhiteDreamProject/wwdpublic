using Robust.Shared.Timing;

namespace Content.Shared._White.Random;

public sealed class PredictedRandom : IPredictedRandom
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
}
