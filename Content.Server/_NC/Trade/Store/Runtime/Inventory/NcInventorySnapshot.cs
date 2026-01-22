namespace Content.Server._NC.Trade;

public sealed class NcInventorySnapshot
{
    public readonly Dictionary<string, int> AncestorCounts = new(StringComparer.Ordinal);
    public readonly Dictionary<string, int> ProtoCounts = new(StringComparer.Ordinal);
    public readonly Dictionary<string, int> StackTypeCounts = new(StringComparer.Ordinal);

    public void Clear()
    {
        ProtoCounts.Clear();
        AncestorCounts.Clear();
        StackTypeCounts.Clear();
    }
}
