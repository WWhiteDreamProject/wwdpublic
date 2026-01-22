using System.Linq;
using Content.Shared._NC.Trade;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server._NC.Trade;

public sealed partial class NcStoreLogicSystem
{
    private readonly Dictionary<string, int> _inheritanceDepthCache = new(StringComparer.Ordinal);

    public MassSellPlan ComputeMassSellPlan(NcStoreComponent store, EntityUid container)
    {
        _inventory.InvalidateInventoryCache(container);
        var cached = _inventory.GetOrBuildDeepItemsCacheCompacted(container);
        return ComputeMassSellPlanInternal(store, cached);
    }

    public MassSellPlan ComputeMassSellPlanFromCachedItems(
        NcStoreComponent store,
        EntityUid container,
        IReadOnlyList<EntityUid> cachedItems
    ) =>
        ComputeMassSellPlanInternal(store, cachedItems);

    public Dictionary<string, int> GetMassSellValue(NcStoreComponent store, EntityUid container) =>
        ComputeMassSellPlan(store, container).IncomeByCurrency;

    private MassSellPlan ComputeMassSellPlanInternal(NcStoreComponent store, IEnumerable<EntityUid> items)
    {
        var incomeByCurrency = new Dictionary<string, int>(StringComparer.Ordinal);
        var unitsByListingId = new Dictionary<string, int>(StringComparer.Ordinal);
        var priceByListingId = new Dictionary<string, (string, int)>(StringComparer.Ordinal);
        var steps = new List<MassSellStep>();
        if (store.Listings.Count == 0)
            return new(incomeByCurrency, unitsByListingId, priceByListingId, steps);

        var stackTypeCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var protoCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var protoCache = new Dictionary<string, EntityPrototype>(StringComparer.Ordinal);
        var stackProtoToType = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var ent in items)
        {
            if (!_ents.EntityExists(ent))
                continue;
            if (_ents.TryGetComponent(ent, out StackComponent? st))
            {
                var cnt = Math.Max(st.Count, 0);
                if (cnt > 0 && !string.IsNullOrWhiteSpace(st.StackTypeId))
                {
                    stackTypeCounts.TryGetValue(st.StackTypeId, out var prev);
                    stackTypeCounts[st.StackTypeId] = prev + cnt;
                }

                if (_ents.TryGetComponent(ent, out MetaDataComponent? stMeta) && stMeta.EntityPrototype is { } stProto)
                {
                    protoCache[stProto.ID] = stProto;
                    if (!string.IsNullOrWhiteSpace(st.StackTypeId))
                        stackProtoToType.TryAdd(stProto.ID, st.StackTypeId);
                }

                continue;
            }

            if (!_ents.TryGetComponent(ent, out MetaDataComponent? meta) || meta.EntityPrototype is null)
                continue;
            var proto = meta.EntityPrototype;
            if (!protoCounts.TryAdd(proto.ID, 1))
                protoCounts[proto.ID] += 1;
            protoCache[proto.ID] = proto;
        }

        if (stackTypeCounts.Count == 0 && protoCounts.Count == 0)
            return new(incomeByCurrency, unitsByListingId, priceByListingId, steps);

        var protoIds = protoCounts.Count > 0 || stackProtoToType.Count > 0
            ? protoCounts.Keys.Concat(stackProtoToType.Keys)
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(GetInheritanceDepth)
                .ThenBy(x => x, OrdinalIds)
                .ToArray()
            : Array.Empty<string>();

        var listingPrices = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var l in store.Listings)
        {
            if (l.Mode != StoreMode.Sell)
                continue;
            if (TryPickCurrencyForSell(store, l, out _, out var price))
                listingPrices[l.Id] = price;
            else
                listingPrices[l.Id] = 0;
        }

        var sellListings = store.Listings
            .Where(l => l.Mode == StoreMode.Sell && !string.IsNullOrEmpty(l.ProductEntity) &&
                l.RemainingCount != 0 && listingPrices.TryGetValue(l.Id, out var p) && p > 0)
            .OrderByDescending(l => listingPrices[l.Id])
            .ThenByDescending(l => GetInheritanceDepth(l.ProductEntity))
            .ThenBy(l => l.ProductEntity, OrdinalIds)
            .ThenBy(l => l.Id, OrdinalIds)
            .ToArray();

        if (sellListings.Length == 0)
            return new(incomeByCurrency, unitsByListingId, priceByListingId, steps);
        var descendantExpected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var l in sellListings)
        {
            if (_inventory.ResolveMatchMode(l.ProductEntity, l.MatchMode) == PrototypeMatchMode.Descendants)
                descendantExpected.Add(l.ProductEntity);

        }

        Dictionary<string, List<string>>? matchesByExpected = null;
        if (descendantExpected.Count > 0 && protoIds.Length > 0)
        {
            matchesByExpected = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var protoId in protoIds)
            {
                if (!protoCache.TryGetValue(protoId, out var proto) && !_protos.TryIndex(protoId, out proto))
                    continue;
                protoCache[protoId] = proto;

                foreach (var anc in _inventory.GetProtoAndAncestors(proto))
                {
                    if (!descendantExpected.Contains(anc))
                        continue;

                    if (!matchesByExpected.TryGetValue(anc, out var list))
                    {
                        list = new List<string>();
                        matchesByExpected[anc] = list;
                    }

                    list.Add(protoId);
                }
            }
        }

var stackName = _compFactory.GetComponentName(typeof(StackComponent));

        foreach (var listing in sellListings)
        {
            if (!TryPickCurrencyForSell(store, listing, out var currencyId, out var unitPrice))
                continue;
            if (unitPrice <= 0 || string.IsNullOrWhiteSpace(currencyId))
                continue;

            var remaining = listing.RemainingCount;
            if (remaining < -1)
                remaining = -1;
            var maxByRemaining = remaining >= 0 ? remaining : int.MaxValue;
            if (maxByRemaining <= 0)
                continue;
            var maxTakeByInt = int.MaxValue / unitPrice;
            if (maxTakeByInt <= 0)
                continue;
            var want = Math.Min(maxByRemaining, maxTakeByInt);

            string? expectedStackType = null;
            if (_protos.TryIndex<EntityPrototype>(listing.ProductEntity, out var prodProto) &&
                prodProto.TryGetComponent(stackName, out StackComponent? prodStackDef))
                expectedStackType = prodStackDef.StackTypeId;

            var taken = 0;
            var effectiveMatch = _inventory.ResolveMatchMode(listing.ProductEntity, listing.MatchMode);

            if (!string.IsNullOrEmpty(expectedStackType))
            {
                if (!stackTypeCounts.TryGetValue(expectedStackType, out var available) || available <= 0)
                    continue;
                taken = Math.Min(available, want);
                stackTypeCounts[expectedStackType] = available - taken;
            }
            else
            {
                if (protoIds.Length == 0)
                    continue;
                if (effectiveMatch != PrototypeMatchMode.Descendants)
                {
                    if (!protoCounts.TryGetValue(listing.ProductEntity, out var available) || available <= 0)
                        continue;
                    taken = Math.Min(available, want);
                    protoCounts[listing.ProductEntity] = available - taken;
                }
                else
                {
                    if (matchesByExpected == null ||
                        !matchesByExpected.TryGetValue(listing.ProductEntity, out var matchingProtoIds) ||
                        matchingProtoIds.Count == 0)
                        continue;

                    foreach (var protoId in matchingProtoIds)
                    {
                        if (taken >= want)
                            break;
                        var isStackProto = stackProtoToType.TryGetValue(protoId, out var stType) &&
                            !string.IsNullOrWhiteSpace(stType);
                        int available;
                        if (isStackProto)
                        {
                            if (!stackTypeCounts.TryGetValue(stType!, out available) || available <= 0)
                                continue;
                        }
                        else
                        {
                            if (!protoCounts.TryGetValue(protoId, out available) || available <= 0)
                                continue;
                        }

                        var take = Math.Min(available, want - taken);
                        if (take <= 0)
                            continue;
                        if (isStackProto)
                            stackTypeCounts[stType!] = available - take;
                        else
                            protoCounts[protoId] = available - take;
                        taken += take;
                    }
                }
            }

            if (taken <= 0)
                continue;
            var total = (long) unitPrice * taken;
            SafeAddIncome(incomeByCurrency, currencyId, total);
            unitsByListingId[listing.Id] = taken;
            priceByListingId[listing.Id] = (currencyId, unitPrice);
            steps.Add(new(listing, currencyId, unitPrice, taken));
        }

        return new(incomeByCurrency, unitsByListingId, priceByListingId, steps);
    }

    private int GetInheritanceDepth(string protoId)
    {
        if (_inheritanceDepthCache.TryGetValue(protoId, out var depth))
            return depth;
        if (!_protos.TryIndex<EntityPrototype>(protoId, out var proto))
        {
            _inheritanceDepthCache[protoId] = 0;
            return 0;
        }

        var max = 0;
        if (proto.Parents != null)
        {
            foreach (var parent in proto.Parents)
            {
                var d = GetInheritanceDepth(parent) + 1;
                if (d > max)
                    max = d;
            }
        }

        _inheritanceDepthCache[protoId] = max;
        return max;
    }

    private static void SafeAddIncome(Dictionary<string, int> income, string currencyId, long delta)
    {
        if (delta <= 0)
            return;
        if (!income.TryGetValue(currencyId, out var cur))
            cur = 0;
        var sum = cur + delta;
        income[currencyId] = sum >= int.MaxValue ? int.MaxValue : (int) sum;
    }

    public readonly record struct MassSellStep(
        NcStoreListingDef Listing,
        string CurrencyId,
        int UnitPrice,
        int Count);

    public readonly record struct MassSellPlan(
        Dictionary<string, int> IncomeByCurrency,
        Dictionary<string, int> UnitsByListingId,
        Dictionary<string, (string CurrencyId, int UnitPrice)> PriceByListingId,
        List<MassSellStep> Steps);
}
