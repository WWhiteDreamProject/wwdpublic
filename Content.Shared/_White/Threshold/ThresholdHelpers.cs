using System.Linq;

namespace Content.Shared._White.Threshold;

public static class ThresholdHelpers
{
    public static TValue? HighestMatch<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TKey key) where TKey : IComparable<TKey> where TValue : struct
    {
        foreach (var (threshold, data) in dictionary.Reverse())
        {
            if (key.CompareTo(threshold) < 0)
                continue;

            return data;
        }

        return null;
    }

    public static TValue? LowestMatch<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TKey key) where TKey : IComparable<TKey> where TValue : struct
    {
        foreach (var (threshold, data) in dictionary)
        {
            if (key.CompareTo(threshold) >= 0)
                continue;

            return data;
        }

        return null;
    }
}
