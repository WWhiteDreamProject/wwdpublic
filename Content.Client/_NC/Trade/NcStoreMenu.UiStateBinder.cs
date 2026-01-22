using Content.Shared._NC.Trade;


namespace Content.Client._NC.Trade;


public sealed partial class NcStoreMenu
{
    private sealed class UiStateBinder
    {
        private readonly NcStoreMenu _m;

        private bool _hasLastDynamic;
        private int _lastContractsHash;
        private int _lastCrateMembershipHash;
        private int _lastReadyMembershipHash;

        public UiStateBinder(NcStoreMenu menu)
        {
            _m = menu;
        }

        private static bool DictEquals(Dictionary<string, int> a, Dictionary<string, int> b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a.Count != b.Count)
                return false;

            foreach (var (k, v) in a)
                if (!b.TryGetValue(k, out var other) || other != v)
                    return false;

            return true;
        }

        private static void ApplySparseSnapshot(Dictionary<string, int> src, Dictionary<string, int> dst)
        {
            dst.Clear();

            foreach (var (k, v) in src)
            {
                if (string.IsNullOrWhiteSpace(k))
                    continue;

                dst[k] = v;
            }
        }

        private static int ComputeContractsHash(List<ContractClientData> contracts)
        {
            unchecked
            {
                var h = 17;
                for (var i = 0; i < contracts.Count; i++)
                {
                    var c = contracts[i];
                    h = h * 31 + (c.Id?.GetHashCode() ?? 0);
                    h = h * 31 + (c.Completed ? 1 : 0);
                    h = h * 31 + c.Progress;
                    h = h * 31 + c.Required;
                    h = h * 31 + (c.Difficulty?.GetHashCode() ?? 0);
                    h = h * 31 + (c.Name?.GetHashCode() ?? 0);
                    h = h * 31 + (c.Targets?.Count ?? 0);
                    h = h * 31 + (c.Rewards?.Count ?? 0);
                }

                return h;
            }
        }

        private int ComputeReadyMembershipHash(Dictionary<string, int> ownedById, Dictionary<string, int> remainingById)
        {
            unchecked
            {
                var h = 17;
                var catalog = _m._catalogModel.Catalog;
                for (var i = 0; i < catalog.Count; i++)
                {
                    var s = catalog[i];
                    if (s.Mode != StoreMode.Sell)
                        continue;

                    var owned = ownedById.GetValueOrDefault(s.Id, 0);
                    if (owned <= 0)
                        continue;

                    var remaining = remainingById.GetValueOrDefault(s.Id, -1);
                    if (remaining == 0)
                        continue;
                    h = h * 31 + (s.Id?.GetHashCode() ?? 0);
                }

                return h;
            }
        }

        private int ComputeCrateMembershipHash(Dictionary<string, int> crateUnitsById)
        {
            unchecked
            {
                var h = 17;

                var catalog = _m._catalogModel.Catalog;
                for (var i = 0; i < catalog.Count; i++)
                {
                    var s = catalog[i];
                    if (s.Mode != StoreMode.Sell)
                        continue;

                    var take = crateUnitsById.GetValueOrDefault(s.Id, 0);
                    if (take <= 0)
                        continue;
                    h = h * 31 + (s.Id?.GetHashCode() ?? 0);
                }

                return h;
            }
        }

        public void PopulateCatalog(
            List<StoreListingStaticData> listings,
            bool hasBuyTab,
            bool hasSellTab,
            bool hasContractsTab
        )
        {
            _m._hasBuyTab = hasBuyTab;
            _m._hasSellTab = hasSellTab;
            _m._hasContractsTab = hasContractsTab;

            _m.ApplyTabsVisibility();
            _m.UpdateHeaderVisibility();

            var filtered = new List<StoreListingStaticData>(listings.Count);

            for (var i = 0; i < listings.Count; i++)
            {
                var s = listings[i];
                if (string.IsNullOrWhiteSpace(s.Id) || string.IsNullOrWhiteSpace(s.ProductEntity))
                    continue;

                filtered.Add(s);
            }

            _m._catalogModel.SetCatalog(filtered);

            var productProtos = new List<string>(filtered.Count);
            for (var i = 0; i < filtered.Count; i++)
                productProtos.Add(filtered[i].ProductEntity);

            _m.BuyView.PrepareSearchIndex(productProtos);
            _m.SellView.PrepareSearchIndex(productProtos);

            _m.RebuildCategoriesFromCatalog();

            _m.BuyView.SetSearch(string.Empty);
            _m.SellView.SetSearch(string.Empty);
            _m.RefreshListings();
            _hasLastDynamic = false;
            _lastContractsHash = 0;
            _lastReadyMembershipHash = 0;
            _lastCrateMembershipHash = 0;
        }

        public void ApplyDynamicState(
            Dictionary<string, int> balancesByCurrency,
            Dictionary<string, int> remainingById,
            Dictionary<string, int> ownedById,
            Dictionary<string, int> crateUnitsById,
            Dictionary<string, int> massTotals,
            bool hasBuyTab,
            bool hasSellTab,
            bool hasContractsTab,
            List<ContractClientData> contracts
        )
        {
            var tabsChanged = !_hasLastDynamic ||
                hasBuyTab != _m._hasBuyTab ||
                hasSellTab != _m._hasSellTab ||
                hasContractsTab != _m._hasContractsTab;

            _m._hasBuyTab = hasBuyTab;
            _m._hasSellTab = hasSellTab;
            _m._hasContractsTab = hasContractsTab;

            if (tabsChanged)
            {
                _m.ApplyTabsVisibility();
                _m.UpdateHeaderVisibility();
            }

            var balancesChanged = !DictEquals(balancesByCurrency, _m._balancesByCurrency);
            if (balancesChanged)
                _m.SetBalancesByCurrency(balancesByCurrency);
            var remainingChanged = !DictEquals(remainingById, _m._catalogModel.RemainingById);
            var ownedChanged = !DictEquals(ownedById, _m._catalogModel.OwnedById);
            var crateChanged = !DictEquals(crateUnitsById, _m._catalogModel.CrateUnitsById);

            if (remainingChanged)
                ApplySparseSnapshot(remainingById, _m._catalogModel.RemainingById);

            if (ownedChanged)
                ApplySparseSnapshot(ownedById, _m._catalogModel.OwnedById);

            if (crateChanged)
                ApplySparseSnapshot(crateUnitsById, _m._catalogModel.CrateUnitsById);
            if (!DictEquals(massTotals, _m._massSellTotals))
                _m.SetMassSellTotals(massTotals);

            var contractsHash = ComputeContractsHash(contracts);
            if (!_hasLastDynamic || contractsHash != _lastContractsHash)
            {
                _lastContractsHash = contractsHash;
                _m.PopulateContracts(contracts);
            }

            var readyMembershipHash = ComputeReadyMembershipHash(ownedById, remainingById);
            var crateMembershipHash = ComputeCrateMembershipHash(crateUnitsById);

            var membershipChanged = !_hasLastDynamic ||
                readyMembershipHash != _lastReadyMembershipHash ||
                crateMembershipHash != _lastCrateMembershipHash;

            var structureChanged = membershipChanged;
            var valuesChanged = remainingChanged || ownedChanged || crateChanged;

            if (structureChanged)
            {
                _m.RebuildItemsFromCatalogAndDynamic();
                _m.UpdateVirtualSellCategories();
                _m.RefreshListings();
            }
            else if (valuesChanged)
            {
                _m._catalogModel.UpdateItemsDynamicInPlace();
                _m.RefreshListingsDynamicOnly();
            }
            else if (balancesChanged || tabsChanged)
                _m.RefreshListingsDynamicOnly();

            _lastReadyMembershipHash = readyMembershipHash;
            _lastCrateMembershipHash = crateMembershipHash;

            _hasLastDynamic = true;
        }
    }
}
