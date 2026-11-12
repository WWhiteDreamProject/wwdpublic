using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared._White.Helpers;

public static class ThresholdHelpers
{
    /// <summary>
    /// Finds the value of the highest threshold that is less than or equal to the given key.
    /// </summary>
    /// <param name="dictionary">The sorted thresholds to search.</param>
    /// <param name="key">The key to compare against the thresholds.</param>
    /// <returns>The matching value, or null if every threshold is greater than the key.</returns>
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

    /// <summary>
    /// Finds the value of the lowest threshold that is greater than or equal to the given key.
    /// </summary>
    /// <param name="dictionary">The sorted thresholds to search.</param>
    /// <param name="key">The key to compare against the thresholds.</param>
    /// <returns>The matching value, or null if every threshold is less than the key.</returns>
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

    /// <summary>
    /// Attempts to find the threshold key associated with the given enum value.
    /// </summary>
    /// <param name="dictionary">The sorted thresholds to search.</param>
    /// <param name="value">The value to look for.</param>
    /// <param name="key">The lowest threshold key mapped to the value, if found.</param>
    /// <returns>True if the value was found, false otherwise.</returns>
    public static bool TryGetKey<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TValue value, [NotNullWhen(true)] out TKey? key) where TKey : IComparable<TKey> where TValue : Enum
    {
        foreach (var (threshold, data) in dictionary)
        {
            if (data.CompareTo(value) != 0)
                continue;

            key = threshold;
            return true;
        }

        key = default;
        return false;
    }

    /// <summary>
    /// Attempts to find the first value, in threshold order, that is greater than the given enum value.
    /// </summary>
    /// <param name="dictionary">The sorted thresholds to search.</param>
    /// <param name="value">The value to compare against.</param>
    /// <param name="nextValue">The next greater value, if found.</param>
    /// <returns>True if a greater value exists, false otherwise.</returns>
    public static bool TryGetNextValue<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TValue value, [NotNullWhen(true)] out TValue? nextValue) where TKey : IComparable<TKey> where TValue : Enum
    {
        foreach (var data in dictionary.Values)
        {
            if (data.CompareTo(value) <= 0)
                continue;

            nextValue = data;
            return true;
        }

        nextValue = default;
        return false;
    }
}
