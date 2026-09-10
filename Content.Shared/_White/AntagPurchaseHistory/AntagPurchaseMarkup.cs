using System.Globalization;
using System.Linq;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.AntagPurchaseHistory;

/// <summary>
/// Shared constants and serialization helpers for purchase icons embedded in round-end markup.
/// </summary>
public static class AntagPurchaseMarkup
{
    public const int IconSize = 24;
    public const string TagName = "antagpurchase";
    public const string FinalCostAttribute = "finalCost";
    public const string OriginalCostAttribute = "originalCost";

    public static string SerializeCost(IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2> cost)
    {
        return string.Join(';', cost
            .OrderBy(pair => pair.Key.Id)
            .Select(pair => $"{pair.Key.Id}:{pair.Value.Value.ToString(CultureInfo.InvariantCulture)}"));
    }

    public static bool TryDeserializeCost(
        string serialized,
        out Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> cost)
    {
        cost = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>();
        if (string.IsNullOrEmpty(serialized))
            return true;

        foreach (var entry in serialized.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = entry.LastIndexOf(':');
            if (separator <= 0 || separator == entry.Length - 1 ||
                !int.TryParse(
                    entry[(separator + 1)..],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var cents))
            {
                cost.Clear();
                return false;
            }

            var currency = new ProtoId<CurrencyPrototype>(entry[..separator]);
            if (!cost.TryAdd(currency, FixedPoint2.FromCents(cents)))
            {
                cost.Clear();
                return false;
            }
        }

        return true;
    }

    public static bool HasDiscount(
        IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2> originalCost,
        IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2> finalCost)
    {
        foreach (var (currency, originalAmount) in originalCost)
        {
            if (!finalCost.TryGetValue(currency, out var finalAmount) || finalAmount < originalAmount)
                return true;
        }

        return false;
    }
}
