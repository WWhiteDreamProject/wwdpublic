using Content.Shared._NC.Trade;
using Robust.Shared.Prototypes;


namespace Content.Server._NC.Trade;


public sealed partial class NcStoreLogicSystem
{
    public bool TryBuy(string listingId, EntityUid machine, NcStoreComponent? store, EntityUid user, int count = 1)
    {
        if (store == null || store.Listings.Count == 0 || count <= 0)
            return false;
        if (!store.ListingIndex.TryGetValue(NcStoreComponent.MakeListingKey(StoreMode.Buy, listingId), out var listing))
            return false;
        if (!_protos.TryIndex<EntityPrototype>(listing.ProductEntity, out var proto))
            return false;

        _inventory.InvalidateInventoryCache(user);
        var snap = _inventory.BuildInventorySnapshot(user);

        if (!TryPickCurrencyForBuy(store, listing, snap, out var currency, out var unitPrice, out var balance))
            return false;

        var unitsPerPurchase = Math.Max(1, listing.UnitsPerPurchase);

        var maxByRemainingPurchases = listing.RemainingCount >= 0 ? listing.RemainingCount : int.MaxValue;
        var maxByMoneyPurchases = unitPrice > 0 ? balance / unitPrice : int.MaxValue;

        var maxPurchases = Math.Min(maxByRemainingPurchases, maxByMoneyPurchases);
        if (maxPurchases <= 0)
            return false;

        var purchases = Math.Min(count, maxPurchases);

        var totalPriceL = (long) unitPrice * purchases;
        if (totalPriceL > int.MaxValue)
            return false;

        var totalUnitsL = (long) purchases * unitsPerPurchase;
        if (totalUnitsL <= 0 || totalUnitsL > int.MaxValue)
            return false;

        var totalPrice = (int) totalPriceL;
        var totalUnits = (int) totalUnitsL;

        if (!TryTakeCurrency(user, currency, totalPrice))
            return false;

        var spawnedUnits = SpawnPurchasedProduct(user, listing.ProductEntity, proto, totalUnits, unitPrice, currency);

        _inventory.InvalidateInventoryCache(user);

        if (spawnedUnits <= 0)
        {
            // Full refund for failed delivery.
            GiveCurrency(user, currency, totalPrice);
            return false;
        }

        var deliveredPurchases = spawnedUnits / unitsPerPurchase;
        if (deliveredPurchases <= 0)
        {
            GiveCurrency(user, currency, totalPrice);
            return false;
        }

        if (deliveredPurchases < purchases)
        {
            var refundPurchases = purchases - deliveredPurchases;
            var refundL = (long) refundPurchases * unitPrice;
            if (refundL > 0 && refundL <= int.MaxValue)
                GiveCurrency(user, currency, (int) refundL);
        }

        if (listing.RemainingCount >= 0)
            listing.RemainingCount = Math.Max(0, listing.RemainingCount - deliveredPurchases);

        Sawmill.Info(
            $"TryBuy: OK {listing.ProductEntity} x{spawnedUnits} ({deliveredPurchases} purchases) for {unitPrice} {currency} each");
        return true;

    }

    public bool TrySell(string listingId, EntityUid machine, NcStoreComponent? store, EntityUid user, int count = 1)
    {
        if (store == null)
            return false;
        return TrySellScenario(listingId, store, user, user, count, out _);
    }

    public bool TrySellFromContainer(
        string listingId,
        EntityUid machine,
        NcStoreComponent? store,
        EntityUid user,
        EntityUid container,
        int count = 1
    )
    {
        if (store == null)
            return false;
        return TrySellScenario(listingId, store, user, container, count, out var sold) &&
            LogSellFromContainer(sold, listingId, store, container);
    }

    private bool TrySellScenario(
        string listingId,
        NcStoreComponent store,
        EntityUid user,
        EntityUid root,
        int count,
        out int sold
    )
    {
        sold = 0;
        if (store.Listings.Count == 0 || count <= 0)
            return false;
        if (!store.ListingIndex.TryGetValue(
            NcStoreComponent.MakeListingKey(StoreMode.Sell, listingId),
            out var listing))
            return false;
        if (!TryPickCurrencyForSell(store, listing, out var currency, out var unitPrice) || unitPrice <= 0)
            return false;

        _inventory.InvalidateInventoryCache(root);

        // Считаем через новую систему
        var snap = _inventory.BuildInventorySnapshot(root);
        var owned = _inventory.GetOwnedFromSnapshot(snap, listing.ProductEntity, listing.MatchMode);

        var maxByRemaining = listing.RemainingCount >= 0 ? listing.RemainingCount : int.MaxValue;
        var maxPossible = Math.Min(owned, maxByRemaining);
        if (maxPossible <= 0)
            return false;

        var actual = Math.Min(count, maxPossible);

        // Забираем через новую систему
        var ok = _inventory.TryTakeProductUnitsFromRootCached(root, listing.ProductEntity, actual, listing.MatchMode);
        if (!ok)
            return false;

        var totalL = (long) unitPrice * actual;
        if (totalL > int.MaxValue)
            return false;

        GiveCurrency(user, currency, (int) totalL);

        _inventory.InvalidateInventoryCache(user);
        if (root != user)
            _inventory.InvalidateInventoryCache(root);

        if (listing.RemainingCount > 0)
            listing.RemainingCount = Math.Max(0, listing.RemainingCount - actual);
        sold = actual;

        if (root == user)
            Sawmill.Info($"TrySell: OK {listing.ProductEntity} x{actual} for {unitPrice} {currency} each");
        return true;
    }

    private bool LogSellFromContainer(int sold, string listingId, NcStoreComponent store, EntityUid container)
    {
        if (sold <= 0)
            return false;
        if (!store.ListingIndex.TryGetValue(
            NcStoreComponent.MakeListingKey(StoreMode.Sell, listingId),
            out var listing))
            return true;
        if (!TryPickCurrencyForSell(store, listing, out var currency, out var unitPrice) || unitPrice <= 0)
            return true;
        Sawmill.Info(
            $"TrySellFromContainer: OK {listing.ProductEntity} x{sold} for {unitPrice} {currency} each (container={ToPrettyString(container)})");
        return true;
    }

    public bool TryExchange(string listingId, EntityUid machine, NcStoreComponent? store, EntityUid user)
    {
        if (store == null || store.Listings.Count == 0)
            return false;
        if (!store.ListingIndex.TryGetValue(
            NcStoreComponent.MakeListingKey(StoreMode.Exchange, listingId),
            out var listing))
            return false;
        if (string.IsNullOrEmpty(listing.ProductEntity))
            return false;

        var requiredCount = listing.RemainingCount > 0 ? listing.RemainingCount : 1;
        if (requiredCount <= 0)
            return false;

        _inventory.InvalidateInventoryCache(user);

        var snap = _inventory.BuildInventorySnapshot(user);
        var owned = _inventory.GetOwnedFromSnapshot(snap, listing.ProductEntity, listing.MatchMode);

        if (owned < requiredCount)
            return false;

        if (!TryPickCurrencyForSell(store, listing, out var currencyId, out var rewardPerUnit) || rewardPerUnit <= 0)
            return false;

        if (!_inventory.TryTakeProductUnitsFromRootCached(
            user,
            listing.ProductEntity,
            requiredCount,
            listing.MatchMode))
            return false;

        var totalRewardL = (long) rewardPerUnit * requiredCount;
        if (totalRewardL > int.MaxValue)
            return false;

        GiveCurrency(user, currencyId, (int) totalRewardL);
        _inventory.InvalidateInventoryCache(user);
        listing.RemainingCount = 0;
        return true;
    }
}
