using Content.Shared._NC.Trade;
using Content.Shared.Stacks;
using Robust.Shared.Containers;


namespace Content.Server._NC.Trade;

public sealed partial class StoreStructuredSystem : EntitySystem
{
    public void UpdateDynamicState(EntityUid uid, NcStoreComponent comp, EntityUid user)
    {
        if (!_ui.IsUiOpen(uid, StoreUiKey.Key, user))
            return;

        EntityUid? crateUid = null;
        if (_logic.TryGetPulledClosedCrate(user, out var pulledCrate))
            crateUid = pulledCrate;

        UpdateStoreWatch(uid, user, crateUid);
        var hasBuyTab = false;
        var hasSellTab = false;
        foreach (var l in comp.Listings)
        {
            if (l.Mode == StoreMode.Buy)
                hasBuyTab = true;
            else if (l.Mode == StoreMode.Sell)
                hasSellTab = true;
            if (hasBuyTab && hasSellTab)
                break;
        }

        var hasContractsTab = comp.ContractPresets.Count > 0;

        var needUserSnap = hasContractsTab;
        if (!needUserSnap && comp.CurrencyWhitelist.Count > 0)
            needUserSnap = true;
        if (!needUserSnap)
        {
            foreach (var l in comp.Listings)
                if (!string.IsNullOrWhiteSpace(l.ProductEntity))
                {
                    needUserSnap = true;
                    break;
                }
        }

        var needCrateScan = crateUid != null && (hasSellTab || hasContractsTab);

        NcInventorySnapshot? userSnap = null;
        NcInventorySnapshot? crateSnap = null;

        if (needUserSnap)
        {
            _inventory.ScanInventory(user, _deepUserItemsScratch, _userSnapScratch);
            userSnap = _userSnapScratch;
        }

        if (needCrateScan && crateUid is { } crateEntity)
        {
            if (hasContractsTab)
            {
                _inventory.ScanInventory(crateEntity, _deepCrateItemsScratch, _crateSnapScratch);
                crateSnap = _crateSnapScratch;
            }
            else
                _inventory.ScanInventoryItems(crateEntity, _deepCrateItemsScratch);
        }

        if (hasContractsTab && userSnap != null)
            UpdateContractsProgress(comp, userSnap, crateSnap);
        var scratch = GetDynamicScratch(uid);
        var buf = scratch.GetWriteBuffer();
        var oldBuf = scratch.GetReadBuffer();

        buf.Clear();

        if (userSnap != null)
        {
            foreach (var cur in comp.CurrencyWhitelist)
            {
                if (string.IsNullOrWhiteSpace(cur))
                    continue;
                buf.BalancesByCurrency[cur] = userSnap.StackTypeCounts.TryGetValue(cur, out var b) ? b : 0;
            }
        }

        foreach (var l in comp.Listings)
        {
            if (string.IsNullOrWhiteSpace(l.Id))
                continue;
            if (l.Mode == StoreMode.Buy && !scratch.ShouldSendBuyDynamicFor(l.Id))
                continue;

            if (l.RemainingCount != -1)
                buf.RemainingById[l.Id] = l.RemainingCount;

            if (userSnap != null && !string.IsNullOrWhiteSpace(l.ProductEntity))
            {
                var owned = _inventory.GetOwnedFromSnapshot(userSnap, l.ProductEntity, l.MatchMode);
                if (owned > 0)
                    buf.OwnedById[l.Id] = owned;
            }
        }

        if (hasSellTab && needCrateScan && crateUid is { } crate)
        {
            var plan = _logic.ComputeMassSellPlanFromCachedItems(comp, crate, _deepCrateItemsScratch);
            foreach (var kvp in plan.UnitsByListingId)
                if (!string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value > 0)
                    buf.CrateUnitsById[kvp.Key] = kvp.Value;
            foreach (var kvp in plan.IncomeByCurrency)
                if (!string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value > 0)
                    buf.CrateTotals[kvp.Key] = kvp.Value;
        }

        if (hasContractsTab && comp.Contracts.Count > 0)
        {
            foreach (var c in comp.Contracts.Values)
                buf.Contracts.Add(MapContractToClient(c));
        }

        if (scratch.EqualsLast(buf, comp.CatalogRevision, hasBuyTab, hasSellTab, hasContractsTab))
            return;

        comp.UiRevision = unchecked(comp.UiRevision + 1);

        _ui.SetUiState(
            uid,
            StoreUiKey.Key,
            new StoreDynamicState(
                comp.UiRevision,
                comp.CatalogRevision,
                buf.BalancesByCurrency,
                buf.RemainingById,
                buf.OwnedById,
                buf.CrateUnitsById,
                buf.CrateTotals,
                buf.Contracts,
                hasBuyTab,
                hasSellTab,
                hasContractsTab
            )
        );

        scratch.Commit(comp.CatalogRevision, hasBuyTab, hasSellTab, hasContractsTab);
    }

    private bool TryFindWatchedRoot(EntityUid start, out EntityUid watchedRoot)
    {
        watchedRoot = default;
        if (_storesByWatchedRoot.Count == 0)
            return false;
        var cur = start;
        for (var i = 0; i < WatchedRootSearchLimit; i++)
        {
            if (_storesByWatchedRoot.TryGetValue(cur, out _))
            {
                watchedRoot = cur;
                return true;
            }

            if (!TryComp(cur, out TransformComponent? xform))
                return false;
            var parent = xform.ParentUid;
            if (parent == EntityUid.Invalid || parent == cur)
                return false;
            cur = parent;
        }

        return false;
    }

    private void RefreshStoresAffectedBy(EntityUid changedRoot)
    {
        if (_storesByWatchedRoot.Count == 0)
            return;

        if (_pendingRefreshEntities.Add(changedRoot))
            _inventory.InvalidateInventoryCache(changedRoot);

        if (_timing.CurTime < _nextCheck && _timing.CurTime >= _nextAccelAllowed)
        {
            _nextCheck = _timing.CurTime;
            _nextAccelAllowed = _timing.CurTime + TimeSpan.FromSeconds(MinAccelInterval);
        }

        if (_pendingRefreshEntities.Count > 4096)
        {
            foreach (var s in _openStoreUids)
            {
                if (_watchByStore.TryGetValue(s, out var watch))
                {
                    if (watch.User != EntityUid.Invalid)
                        _inventory.InvalidateInventoryCache(watch.User);
                    if (watch.Crate is { } crate)
                        _inventory.InvalidateInventoryCache(crate);
                }

                MarkDirty(s);
            }

            _pendingRefreshEntities.Clear();
        }
    }

    private void OnUserEntInserted(EntityUid uid, ContainerManagerComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (_storesByWatchedRoot.Count == 0)
            return;

        if (TryFindWatchedRoot(uid, out var r))
            RefreshStoresAffectedBy(r);
    }

    private void OnUserEntRemoved(EntityUid uid, ContainerManagerComponent comp, EntRemovedFromContainerMessage args)
    {
        if (_storesByWatchedRoot.Count == 0)
            return;

        if (TryFindWatchedRoot(uid, out var r))
            RefreshStoresAffectedBy(r);
    }

    private void OnStackCountChanged(EntityUid uid, StackComponent comp, ref StackCountChangedEvent args)
    {
        if (_storesByWatchedRoot.Count == 0)
            return;

        if (TryFindWatchedRoot(uid, out var r))
            RefreshStoresAffectedBy(r);
    }


    private void ProcessPendingRefreshes()
    {
        if (_pendingRefreshEntities.Count == 0 || _storesByWatchedRoot.Count == 0)
            return;
        _affectedStoresScratch.Clear();
        foreach (var root in _pendingRefreshEntities)
        {
            if (!Exists(root))
                continue;
            if (_storesByWatchedRoot.TryGetValue(root, out var stores))
            {
                foreach (var s in stores)
                    _affectedStoresScratch.Add(s);
            }
        }

        _pendingRefreshEntities.Clear();
        foreach (var s in _affectedStoresScratch)
            MarkDirty(s);
    }

    private void UpdateContractsProgress(
        NcStoreComponent comp,
        NcInventorySnapshot UserSnap,
        NcInventorySnapshot? CrateSnap
    )
    {
        if (comp.Contracts.Count == 0)
            return;

        foreach (var (_, contract) in comp.Contracts)
        {
            var targets = contract.Targets;

            if (targets.Count > 0)
            {
                var totalRequired = 0;
                var totalProgress = 0;

                foreach (var t in targets)
                {
                    if (string.IsNullOrWhiteSpace(t.TargetItem) || t.Required <= 0)
                    {
                        t.Progress = 0;
                        continue;
                    }

                    var owned = _logic.GetOwnedFromSnapshot(UserSnap, t.TargetItem, t.MatchMode);

                    if (CrateSnap != null)
                        owned += _logic.GetOwnedFromSnapshot(CrateSnap, t.TargetItem, t.MatchMode);

                    var prog = Math.Min(owned, t.Required);
                    t.Progress = prog;

                    totalRequired += t.Required;
                    totalProgress += prog;
                }

                contract.Required = totalRequired;
                contract.Progress = totalProgress;

                if (targets.Count > 0)
                    contract.TargetItem = targets[0].TargetItem;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(contract.TargetItem) || contract.Required <= 0)
                {
                    contract.Progress = 0;
                    continue;
                }

                var owned = _logic.GetOwnedFromSnapshot(UserSnap, contract.TargetItem, contract.MatchMode);

                if (CrateSnap != null)
                    owned += _logic.GetOwnedFromSnapshot(CrateSnap, contract.TargetItem, contract.MatchMode);

                contract.Progress = Math.Min(owned, contract.Required);
            }
        }
    }
}
