using Content.Shared._NC.Trade;
using Robust.Shared.Prototypes;


namespace Content.Server._NC.Trade;


public sealed class StoreSystemStructuredLoader : EntitySystem
{
    private static readonly ISawmill Sawmill = Logger.GetSawmill("ncstore-loader");

    [Dependency] private readonly NcContractSystem _contracts = default!;

    private readonly HashSet<EntityUid> _contractsInitialized = new();
    private readonly HashSet<EntityUid> _loadedStores = new();
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NcStoreComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NcStoreComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NcStoreComponent, EntityTerminatingEvent>(OnTerminating);
    }

    private void OnTerminating(EntityUid uid, NcStoreComponent comp, ref EntityTerminatingEvent args)
    {
        _loadedStores.Remove(uid);
        _contractsInitialized.Remove(uid);
    }

    private void OnMapInit(EntityUid uid, NcStoreComponent comp, MapInitEvent args) =>
        EnsureLoadedInternal(uid, comp, "MapInit", true);

    public void EnsureLoaded(EntityUid uid, NcStoreComponent comp, string reason) =>
        EnsureLoadedInternal(uid, comp, reason, true);

    private void OnStartup(EntityUid uid, NcStoreComponent comp, ComponentStartup args) =>
        EnsureLoadedInternal(uid, comp, "Startup", true);

    private void EnsureLoadedInternal(EntityUid uid, NcStoreComponent comp, string reason, bool allowContractsInit)
    {
        var changed = false;

        if (comp.Listings.Count == 0)
        {
            TryLoadPresets(uid, comp, reason);
            if (comp.Listings.Count > 0)
                changed = true;
        }

        if (comp.Listings.Count > 0 && comp.ListingIndex.Count == 0)
        {
            comp.RebuildListingIndex();
            changed = true;
        }

        if (_loadedStores.Add(uid))
            changed = true;

        if (changed)
            comp.BumpCatalogRevision();

        if (allowContractsInit && !_contractsInitialized.Contains(uid))
        {
            _contracts.RefillContractsForStore(uid, comp);
            _contractsInitialized.Add(uid);
        }
    }

    private void TryLoadPresets(EntityUid uid, NcStoreComponent comp, string reason)
    {
        if (comp.BuyPresets.Count == 0 && comp.SellPresets.Count == 0)
        {
            Sawmill.Warning($"[NcStore] {ToPrettyString(uid)}: нет ни одного пресета (reason={reason})");
            return;
        }

        comp.CurrencyWhitelist.Clear();
        comp.Categories.Clear();
        comp.Listings.Clear();
        comp.ListingIndex.Clear();

        var ctx = new LoadContext();

        var total = 0;

        foreach (var id in comp.BuyPresets)
            total += LoadPresetForMode(id, StoreMode.Buy, comp, ctx);

        foreach (var id in comp.SellPresets)
            total += LoadPresetForMode(id, StoreMode.Sell, comp, ctx);

        if (total == 0)
        {
            Sawmill.Warning($"[NcStore] {ToPrettyString(uid)}: ни одного лота не загружено (reason={reason})");
            return;
        }

        Sawmill.Info(
            $"[NcStore] {ToPrettyString(uid)}: загружено {total} лотов. " +
            $"BuyPresets=[{string.Join(", ", comp.BuyPresets)}], " +
            $"SellPresets=[{string.Join(", ", comp.SellPresets)}], reason={reason}");
    }

    private int LoadPresetForMode(string presetId, StoreMode mode, NcStoreComponent comp, LoadContext ctx)
    {
        if (!_prototypes.TryIndex<StorePresetStructuredPrototype>(presetId, out var preset))
        {
            Sawmill.Error($"[NcStore] Пресет '{presetId}' не найден");
            return 0;
        }

        var count = 0;

        if (!string.IsNullOrWhiteSpace(preset.Currency) && ctx.CurrencySeen.Add(preset.Currency))
            comp.CurrencyWhitelist.Add(preset.Currency);

        foreach (var categoryId in preset.Categories)
        {
            if (!_prototypes.TryIndex<StoreCategoryStructuredPrototype>(categoryId, out var categoryProto))
            {
                Sawmill.Error($"[NcStore] Категория '{categoryId}' не найдена (preset='{presetId}')");
                continue;
            }

            var categoryName = categoryProto.Name;

            if (ctx.CategorySeen.Add(categoryName))
                comp.Categories.Add(categoryName);

            foreach (var entry in categoryProto.Entries)
            {
                var baseId = $"{presetId}:{mode}:{categoryId}:{entry.Proto}";
                var id = AllocateDeterministicId(baseId, ctx);

                var listing = new NcStoreListingDef
                {
                    Id = id,
                    ProductEntity = entry.Proto,
                    MatchMode = entry.MatchMode,
                    Mode = mode,
                    Categories = new List<string> { categoryName },
                    Conditions = new List<ListingConditionPrototype>(),
                    RemainingCount = entry.Count ?? -1,
                    UnitsPerPurchase = Math.Max(1, entry.Amount),
                    Cost = new()
                };

                if (!string.IsNullOrWhiteSpace(preset.Currency))
                    listing.Cost[preset.Currency] = entry.Price;

                comp.Listings.Add(listing);
                count++;
            }
        }

        return count;
    }

    private static string AllocateDeterministicId(string baseId, LoadContext ctx)
    {
        if (!ctx.NextSuffixByBaseId.TryGetValue(baseId, out var nextSuffix))
        {
            if (ctx.ListingIds.Add(baseId))
            {
                ctx.NextSuffixByBaseId[baseId] = 1;
                return baseId;
            }

            nextSuffix = 1;
        }

        while (true)
        {
            var candidate = $"{baseId}#{nextSuffix}";
            if (ctx.ListingIds.Add(candidate))
            {
                ctx.NextSuffixByBaseId[baseId] = nextSuffix + 1;
                return candidate;
            }

            nextSuffix++;
        }
    }

    private sealed class LoadContext
    {
        public readonly HashSet<string> CategorySeen = new(StringComparer.Ordinal);
        public readonly HashSet<string> CurrencySeen = new(StringComparer.Ordinal);
        public readonly HashSet<string> ListingIds = new(StringComparer.Ordinal);
        public readonly Dictionary<string, int> NextSuffixByBaseId = new(StringComparer.Ordinal);
    }
}
