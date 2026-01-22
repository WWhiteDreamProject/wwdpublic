using Content.Shared._NC.Trade;
using Robust.Shared.Random;

namespace Content.Server._NC.Trade;

public sealed partial class NcContractSystem : EntitySystem
{
    private const int SmallBagThreshold = 8;
    private const int MaxRngCache = 4096;

    private double NextUnit() => _random.NextFloat();

    private static bool TryNormalizeRange(
        IntRange range,
        int minClamp,
        int maxClamp,
        out int min,
        out int buckets)
    {
        var a = range.Min;
        var b = range.Max;

        if (b < a)
            (a, b) = (b, a);

        a = Math.Clamp(a, minClamp, maxClamp);
        b = Math.Clamp(b, minClamp, maxClamp);

        if (b <= a)
        {
            min = a;
            buckets = 1;
            return false;
        }

        min = a;
        buckets = b - a + 1;
        return true;
    }

    private int RollSmooth(in QuasiKey key, int min, int buckets, double jitter)
    {
        if (_quasiPhase.Count >= MaxRngCache)
            _quasiPhase.Clear();

        if (!_quasiPhase.TryGetValue(key, out var p))
            p = NextUnit();

        var j = (NextUnit() - 0.5) * 2.0 * jitter;

        p = p + Golden + j;
        p -= Math.Floor(p);
        _quasiPhase[key] = p;

        var idx = (int)Math.Floor(p * buckets);
        if ((uint)idx >= (uint)buckets)
            idx = buckets - 1;

        return min + idx;
    }

    private int RollFair(
        QuasiKey key,
        IntRange range,
        int minClamp,
        int maxClamp = int.MaxValue,
        double jitter = DefaultJitter)
    {
        if (!TryNormalizeRange(range, minClamp, maxClamp, out var min, out var buckets))
            return min;

        return buckets <= SmallBagThreshold
            ? RollFromSmallBag(key, min, buckets)
            : RollSmooth(key, min, buckets, jitter);
    }

    private int RollFromSmallBag(QuasiKey key, int min, int buckets)
    {
        if (_smallBags.Count >= MaxRngCache)
            _smallBags.Clear();

        if (!_smallBags.TryGetValue(key, out var state))
        {
            state = new();
            _smallBags[key] = state;
        }

        var max = min + buckets - 1;

        var needsReset =
            state.Min != min ||
            state.Max != max ||
            state.Order.Count != buckets ||
            state.Cursor >= state.Order.Count;

        if (needsReset)
        {
            state.Min = min;
            state.Max = max;
            state.Cursor = 0;
            state.Order.Clear();

            for (var i = 0; i < buckets; i++)
                state.Order.Add(i);

            for (var i = buckets - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
                (state.Order[i], state.Order[j]) = (state.Order[j], state.Order[i]);
            }

            if (state.LastIdx >= 0 && buckets > 1 && state.Order[0] == state.LastIdx)
                (state.Order[0], state.Order[1]) = (state.Order[1], state.Order[0]);
        }

        var idx = state.Order[state.Cursor++];
        state.LastIdx = idx;

        return min + idx;
    }

    private static T PickWeighted<T>(
        IRobustRandom random,
        IReadOnlyList<T> list,
        Func<T, int> weightSelector)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("PickWeighted called with empty list.");

        long total = 0;

        var weights = list.Count <= 128
            ? stackalloc int[list.Count]
            : new int[list.Count];

        for (var i = 0; i < list.Count; i++)
        {
            var w = weightSelector(list[i]);
            if (w < 0)
                w = 0;

            weights[i] = w;
            total += w;
        }

        if (total <= 0)
            return list[random.Next(list.Count)];

        var r = total <= int.MaxValue
            ? random.Next((int)total)
            : (long)(random.NextDouble() * total);

        long acc = 0;
        for (var i = 0; i < list.Count; i++)
        {
            var w = weights[i];
            if (w <= 0)
                continue;

            acc += w;
            if (r < acc)
                return list[i];
        }

        for (var i = list.Count - 1; i >= 0; i--)
            if (weights[i] > 0)
                return list[i];

        return list[^1];
    }
}
