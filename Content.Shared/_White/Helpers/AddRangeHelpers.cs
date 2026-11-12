namespace Content.Shared._White.Helpers;

public static class AddRangeHelpers
{
    /// <summary>
    /// Adds all given items to the set.
    /// </summary>
    /// <param name="set">The set to add the items to.</param>
    /// <param name="items">The items to add.</param>
    /// <returns>True if every item was newly added, false if at least one was already present.</returns>
    public static bool AddRange<T>(this HashSet<T> set, IEnumerable<T> items)
    {
        var allAdded = true;
        foreach (var item in items)
        {
            allAdded &= set.Add(item);
        }
        return allAdded;
    }
}
