using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared._White.Threshold;

public static class ThresholdHelpers
{
    public static TKey? GetKey<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TValue value) where TKey : IComparable<TKey> where TValue : Enum
    {
        foreach (var (threshold, data) in dictionary)
        {
            if (data.CompareTo(value) != 0)
                continue;

            return threshold;
        }

        return default;
    }

    public static TValue? GetNextValue<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TValue value) where TKey : IComparable<TKey> where TValue : Enum
    {
        foreach (var data in dictionary.Values)
        {
            if (data.CompareTo(value) <= 0)
                continue;

            return data;
        }

        return default;
    }

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
            if (key.CompareTo(threshold) > 0)
                continue;

            return data;
        }

        return null;
    }

    public static bool TryGetKey<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TValue value, [NotNullWhen(true)] out TKey? key) where TKey : IComparable<TKey> where TValue : Enum
    {
        key = dictionary.GetKey(value);
        return key != null;
    }

    public static bool TryGetNextValue<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TValue value, [NotNullWhen(true)] out TValue? nextValue) where TKey : IComparable<TKey> where TValue : Enum
    {
        nextValue = dictionary.GetNextValue(value);
        return nextValue != null;
    }
}
