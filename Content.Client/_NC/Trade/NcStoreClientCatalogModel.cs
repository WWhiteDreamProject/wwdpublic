using Content.Shared._NC.Trade;

namespace Content.Client._NC.Trade;

public sealed class NcStoreClientCatalogModel
{
    public const string CatIdReady = "nc.internal.category.ready";
    public const string CatIdCrate = "nc.internal.category.crate";

    private readonly List<StoreListingStaticData> _catalog = new();
    private readonly List<StoreListingData> _items = new();
    private readonly List<StoreListingData> _scratchReadyItems = new();
    private readonly HashSet<string> _availableIds = new();

    public IReadOnlyList<StoreListingStaticData> Catalog => _catalog;
    public IReadOnlyList<StoreListingData> Items => _items;

    public Dictionary<string, int> RemainingById { get; } = new();
    public Dictionary<string, int> OwnedById { get; } = new();
    public Dictionary<string, int> CrateUnitsById { get; } = new();

    public HashSet<string> AvailableIds => _availableIds;

    public void SetCatalog(List<StoreListingStaticData> catalog)
    {
        _catalog.Clear();
        _catalog.AddRange(catalog);
    }

    public void Clear()
    {
        _catalog.Clear();
        _items.Clear();
        _scratchReadyItems.Clear();
        _availableIds.Clear();
        RemainingById.Clear();
        OwnedById.Clear();
        CrateUnitsById.Clear();
    }


    public void RebuildItemsFromCatalogAndDynamic()
    {
        _items.Clear();

        for (var i = 0; i < _catalog.Count; i++)
        {
            var s = _catalog[i];

            var remaining = RemainingById.GetValueOrDefault(s.Id, -1);
            var owned = OwnedById.GetValueOrDefault(s.Id, 0);

            _items.Add(
                new(
                    s.Id,
                    StoreListingFlavor.Base,
                    s.ProductEntity,
                    s.BasePrice,
                    s.Category,
                    s.CurrencyId,
                    s.Mode,
                    owned,
                    remaining,
                    s.UnitsPerPurchase));
        }

        var baseCount = _items.Count;
        _scratchReadyItems.Clear();

        for (var i = 0; i < baseCount; i++)
        {
            var d = _items[i];
            if (d.Mode != StoreMode.Sell)
                continue;

            if (d.Owned <= 0)
                continue;

            if (d.Remaining == 0)
                continue;

            _scratchReadyItems.Add(
                new(
                    d.ListingId,
                    StoreListingFlavor.Ready,
                    d.ProductEntity,
                    d.Price,
                    CatIdReady,
                    d.CurrencyId,
                    d.Mode,
                    d.Owned,
                    d.Remaining,
                    d.UnitsPerPurchase));
        }

        if (_scratchReadyItems.Count > 0)
            _items.AddRange(_scratchReadyItems);

        if (CrateUnitsById.Count > 0)
        {
            for (var i = 0; i < _catalog.Count; i++)
            {
                var s = _catalog[i];
                if (s.Mode != StoreMode.Sell)
                    continue;

                if (!CrateUnitsById.TryGetValue(s.Id, out var take) || take <= 0)
                    continue;

                var remaining = RemainingById.GetValueOrDefault(s.Id, -1);

                _items.Add(
                    new(
                        s.Id,
                        StoreListingFlavor.Crate,
                        s.ProductEntity,
                        s.BasePrice,
                        CatIdCrate,
                        s.CurrencyId,
                        StoreMode.Sell,
                        take,
                        remaining,
                        s.UnitsPerPurchase));
            }
        }

        _availableIds.Clear();
        for (var i = 0; i < _items.Count; i++)
            _availableIds.Add(_items[i].Id);
    }

    public void UpdateItemsDynamicInPlace()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            var it = _items[i];
            var listingId = it.ListingId;

            if (it.Flavor == StoreListingFlavor.Crate)
            {
                it.Owned = CrateUnitsById.GetValueOrDefault(listingId, 0);
                it.Remaining = RemainingById.GetValueOrDefault(listingId, -1);
                continue;
            }

            it.Owned = OwnedById.GetValueOrDefault(listingId, 0);
            it.Remaining = RemainingById.GetValueOrDefault(listingId, -1);
        }
    }
}
