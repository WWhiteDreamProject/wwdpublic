using System.Linq;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Shared._NC.Trade;
using Content.Shared.Access.Components;
using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Timing;


namespace Content.Server._NC.Trade;


public sealed partial class StoreStructuredSystem : EntitySystem
{
    private const float AutoCloseDistance = 3f;
    private const float MinAccelInterval = 0.25f;
    private const float MinDynamicInterval = 0.25f;
    private const int MaxVisibleListingIds = 256;
    private const int MaxVisibleListingIdLength = 96;
    private const int WatchedRootSearchLimit = 32;
    private const float CheckInterval = 1.0f;
    private readonly HashSet<EntityUid> _affectedStoresScratch = new();
    [Dependency] private readonly AudioSystem _audio = default!;
    private readonly Dictionary<EntityUid, (int Revision, List<StoreListingStaticData> List)> _catalogCache = new();
    [Dependency] private readonly NcContractSystem _contracts = default!;
    private readonly NcInventorySnapshot _crateSnapScratch = new();
    private readonly List<EntityUid> _deepCrateItemsScratch = new();
    private readonly List<EntityUid> _deepUserItemsScratch = new();
    private readonly HashSet<EntityUid> _dirtyStores = new();
    private readonly List<EntityUid> _dirtyStoresScratch = new();
    private readonly Dictionary<EntityUid, DynamicScratch> _dynamicScratchByStore = new();
    [Dependency] private readonly NcStoreInventorySystem _inventory = default!;
    [Dependency] private readonly StoreSystemStructuredLoader _loader = default!;
    [Dependency] private readonly NcStoreLogicSystem _logic = default!;
    private readonly List<EntityUid> _openStoresScratch = new();
    private readonly HashSet<EntityUid> _openStoreUids = new();
    private readonly HashSet<EntityUid> _pendingRefreshEntities = new();
    [Dependency] private readonly PopupSystem _popups = default!;
    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _storesByWatchedRoot = new();
    [Dependency] private readonly NcStoreSystem _storeSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    private readonly NcInventorySnapshot _userSnapScratch = new();
    private readonly Dictionary<EntityUid, (EntityUid User, EntityUid? Crate)> _watchByStore = new();

    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private TimeSpan _nextAccelAllowed = TimeSpan.Zero;
    private TimeSpan _nextCheck = TimeSpan.Zero;
    private const int MaxDynamicUpdatesPerTick = 8;

    private DynamicScratch GetDynamicScratch(EntityUid storeUid)
    {
        if (_dynamicScratchByStore.TryGetValue(storeUid, out var scratch))
            return scratch;

        scratch = new();
        _dynamicScratchByStore[storeUid] = scratch;
        return scratch;
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NcStoreComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);
        SubscribeLocalEvent<NcStoreComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<NcStoreComponent, RequestUiRefreshMessage>(OnUiRefreshRequest);
        SubscribeLocalEvent<NcStoreComponent, StoreSetVisibleListingsBoundUiMessage>(OnSetVisibleListings);
        SubscribeLocalEvent<AccessReaderComponent, AccessReaderConfigurationChangedEvent>(OnAccessReaderChanged);
        SubscribeLocalEvent<NcStoreComponent, ComponentShutdown>(OnStoreShutdown);
        SubscribeLocalEvent<ContainerManagerComponent, EntInsertedIntoContainerMessage>(OnUserEntInserted);
        SubscribeLocalEvent<ContainerManagerComponent, EntRemovedFromContainerMessage>(OnUserEntRemoved);
        SubscribeLocalEvent<StackComponent, StackCountChangedEvent>(OnStackCountChanged);
        SubscribeLocalEvent<NcStoreComponent, ClaimContractBoundMessage>(OnClaimContract);
        SubscribeLocalEvent<EntityStorageComponent, StorageAfterOpenEvent>(OnStorageOpen);
        SubscribeLocalEvent<EntityStorageComponent, StorageAfterCloseEvent>(OnStorageClose);
    }


    private bool TryGetLockedUiUser(EntityUid store, NcStoreComponent comp, out EntityUid user)
    {
        user = default;
        if (comp.CurrentUser is not { } cur || cur == EntityUid.Invalid)
            return false;

        if (!_ui.IsUiOpen(store, StoreUiKey.Key, cur))
            return false;

        user = cur;
        return true;
    }


    private void OnSetVisibleListings(EntityUid uid, NcStoreComponent comp, StoreSetVisibleListingsBoundUiMessage msg)
    {
        if (!TryGetLockedUiUser(uid, comp, out var user))
            return;

        var ids = msg.Ids;
        if (ids.Length > MaxVisibleListingIds)
            ids = ids.Take(MaxVisibleListingIds).ToArray();

        if (ids.Length > 0)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                if (id.Length is 0 or > MaxVisibleListingIdLength)
                    ids[i] = string.Empty;
            }
        }

        var scratch = GetDynamicScratch(uid);
        if (!scratch.UpdateVisibleIds(ids))
            return;
        MarkDirty(uid);

        var now = _timing.CurTime;
        if (now >= scratch.NextDynamicAllowed)
        {
            _dirtyStores.Remove(uid);
            UpdateDynamicState(uid, comp, user);
            scratch.NextDynamicAllowed = now + TimeSpan.FromSeconds(MinDynamicInterval);
        }
    }

    private void OnStorageOpen(EntityUid uid, EntityStorageComponent comp, ref StorageAfterOpenEvent args)
    {
        if (_storesByWatchedRoot.ContainsKey(uid))
            RefreshStoresAffectedBy(uid);
    }

    private void OnStorageClose(EntityUid uid, EntityStorageComponent comp, ref StorageAfterCloseEvent args)
    {
        if (_storesByWatchedRoot.ContainsKey(uid))
            RefreshStoresAffectedBy(uid);
    }

    private void OnStoreShutdown(EntityUid uid, NcStoreComponent comp, ComponentShutdown args)
    {
        _catalogCache.Remove(uid);
        _dynamicScratchByStore.Remove(uid);

        if (_openStoreUids.Contains(uid) || _watchByStore.ContainsKey(uid) || _dirtyStores.Contains(uid))
        {
            EntityUid? user = null;

            if (_watchByStore.TryGetValue(uid, out var watch) && watch.User != EntityUid.Invalid)
                user = watch.User;
            else if (comp.CurrentUser is { } cur && cur != EntityUid.Invalid)
                user = cur;

            CloseAndCleanUp(uid, user);
        }
    }

    public void RefreshCatalog(EntityUid uid, NcStoreComponent comp)
    {
        _catalogCache.Remove(uid);
        _dynamicScratchByStore.Remove(uid);

        comp.BumpCatalogRevision();

        if (comp.CurrentUser is not { } user)
            return;

        if (!_ui.IsUiOpen(uid, StoreUiKey.Key, user))
            return;

        SendCatalog(uid, comp, user);
        UpdateDynamicState(uid, comp, user);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        ProcessPendingRefreshes();

        if (_openStoreUids.Count > 0)
        {
            _openStoresScratch.Clear();
            _openStoresScratch.AddRange(_openStoreUids);

            foreach (var uid in _openStoresScratch)
            {
                if (!TryComp(uid, out NcStoreComponent? store) || store.CurrentUser is not { } user)
                    continue;

                if (EnsureCrateWatchUpToDate(uid, user))
                    MarkDirty(uid);
            }
        }

        if (_dirtyStores.Count > 0)
        {
            var now = _timing.CurTime;

            _dirtyStoresScratch.Clear();
            _dirtyStoresScratch.AddRange(_dirtyStores);

            var processed = 0;

            foreach (var uid in _dirtyStoresScratch)
            {
                if (processed >= MaxDynamicUpdatesPerTick)
                    break;

                if (!TryComp(uid, out NcStoreComponent? store) || store.CurrentUser is not { } user)
                {
                    _dirtyStores.Remove(uid);
                    continue;
                }

                var scratch = GetDynamicScratch(uid);

                if (now < scratch.NextDynamicAllowed)
                    continue;

                UpdateDynamicState(uid, store, user);

                scratch.NextDynamicAllowed = now + TimeSpan.FromSeconds(MinDynamicInterval);
                _dirtyStores.Remove(uid);

                processed++;
            }
        }

        if (_timing.CurTime < _nextCheck)
            return;

        _nextCheck = _timing.CurTime + TimeSpan.FromSeconds(CheckInterval);

        if (_openStoreUids.Count == 0)
            return;
        _openStoresScratch.Clear();
        _openStoresScratch.AddRange(_openStoreUids);

        foreach (var uid in _openStoresScratch)
        {
            if (!TryComp(uid, out NcStoreComponent? store) || !TryComp(uid, out TransformComponent? xform))
            {
                CloseAndCleanUp(uid);
                continue;
            }

            if (store.CurrentUser is not { } userUid)
            {
                CloseAndCleanUp(uid);
                continue;
            }

            if (!TryComp(userUid, out TransformComponent? userXform) ||
                !_xform.InRange(xform.Coordinates, userXform.Coordinates, AutoCloseDistance))
            {
                CloseAndCleanUp(uid, userUid);
                store.CurrentUser = null;
                continue;
            }

            if (!_storeSystem.CanUseStore(uid, store, userUid))
            {
                CloseAndCleanUp(uid, userUid);
                store.CurrentUser = null;
                _popups.PopupEntity(Loc.GetString("nc-store-no-access"), uid, userUid);
            }
        }
    }

    private void CloseAndCleanUp(EntityUid storeUid, EntityUid? user = null)
    {
        if (_watchByStore.TryGetValue(storeUid, out var info))
        {
            if (info.User != EntityUid.Invalid)
                _inventory.InvalidateInventoryCache(info.User);

            if (info.Crate is { } crate)
                _inventory.InvalidateInventoryCache(crate);
        }

        if (user != null)
            _ui.CloseUi(storeUid, StoreUiKey.Key, user.Value);

        if (_dynamicScratchByStore.TryGetValue(storeUid, out var scratch))
            scratch.UpdateVisibleIds(null);
        _openStoreUids.Remove(storeUid);
        UnregisterStoreWatch(storeUid);
        _dirtyStores.Remove(storeUid);
        _dynamicScratchByStore.Remove(storeUid);
    }

    private bool EnsureCrateWatchUpToDate(EntityUid storeUid, EntityUid user)
    {
        EntityUid? crateUid = null;
        if (_logic.TryGetPulledClosedCrate(user, out var pulledCrate))
            crateUid = pulledCrate;
        if (_watchByStore.TryGetValue(storeUid, out var prev))
        {
            if (prev.User == user && prev.Crate == crateUid)
                return false;
            if (prev.Crate != crateUid)
            {
                if (prev.Crate is { } oldCrate)
                    _inventory.InvalidateInventoryCache(oldCrate);
                if (crateUid is { } newCrate)
                    _inventory.InvalidateInventoryCache(newCrate);
            }

            if (prev.User != user)
            {
                if (prev.User != EntityUid.Invalid)
                    _inventory.InvalidateInventoryCache(prev.User);
                _inventory.InvalidateInventoryCache(user);
            }
        }
        else
        {
            _inventory.InvalidateInventoryCache(user);
            if (crateUid is { } newCrate)
                _inventory.InvalidateInventoryCache(newCrate);
        }

        UpdateStoreWatch(storeUid, user, crateUid);
        return true;
    }

    private void AddWatchedRoot(EntityUid root, EntityUid storeUid)
    {
        if (!_storesByWatchedRoot.TryGetValue(root, out var set))
        {
            set = new();
            _storesByWatchedRoot[root] = set;
        }

        set.Add(storeUid);
    }

    private void RemoveWatchedRoot(EntityUid root, EntityUid storeUid)
    {
        if (!_storesByWatchedRoot.TryGetValue(root, out var set))
            return;
        set.Remove(storeUid);
        if (set.Count == 0)
            _storesByWatchedRoot.Remove(root);
    }

    private void UpdateStoreWatch(EntityUid storeUid, EntityUid user, EntityUid? crate)
    {
        if (user == EntityUid.Invalid)
        {
            UnregisterStoreWatch(storeUid);
            return;
        }

        if (_watchByStore.TryGetValue(storeUid, out var prev))
        {
            if (prev.User == user && prev.Crate == crate)
                return;
            if (prev.User != EntityUid.Invalid)
                RemoveWatchedRoot(prev.User, storeUid);
            if (prev.Crate is { } oldCrate)
                RemoveWatchedRoot(oldCrate, storeUid);
        }

        _watchByStore[storeUid] = (user, crate);
        AddWatchedRoot(user, storeUid);
        _inventory.InvalidateInventoryCache(user);
        if (crate is { } c)
        {
            AddWatchedRoot(c, storeUid);
            _inventory.InvalidateInventoryCache(c);
        }
    }

    private void UnregisterStoreWatch(EntityUid storeUid)
    {
        if (!_watchByStore.TryGetValue(storeUid, out var info))
            return;
        if (info.User != EntityUid.Invalid)
            RemoveWatchedRoot(info.User, storeUid);
        if (info.Crate is { } crate)
            RemoveWatchedRoot(crate, storeUid);
        _watchByStore.Remove(storeUid);
    }

    private void OnUiOpenAttempt(EntityUid uid, NcStoreComponent comp, ref ActivatableUIOpenAttemptEvent ev)
    {
        ev.Cancel();
        var user = ev.User;

        if (!_ui.HasUi(uid, StoreUiKey.Key))
            return;
        if (!_storeSystem.CanUseStore(uid, comp, user))
            return;
        if (comp.CurrentUser is { } current && current != user)
            return;
        if (TryComp(uid, out TransformComponent? sX) && TryComp(user, out TransformComponent? uX) &&
            !_xform.InRange(sX.Coordinates, uX.Coordinates, AutoCloseDistance))
            return;

        var wasInUse = comp.CurrentUser != null;
        comp.CurrentUser = user;
        if (!wasInUse)
            _openStoreUids.Add(uid);

        if (!_ui.IsUiOpen(uid, StoreUiKey.Key, user))
            _ui.OpenUi(uid, StoreUiKey.Key, user);

        EnsureCrateWatchUpToDate(uid, user);

        _loader.EnsureLoaded(uid, comp, "UiOpenAttempt");

        SendCatalog(uid, comp, user);
        UpdateDynamicState(uid, comp, user);
    }


    private void SendCatalog(EntityUid store, NcStoreComponent comp, EntityUid user)
    {
        if (!_ui.IsUiOpen(store, StoreUiKey.Key, user))
            return;

        if (_catalogCache.TryGetValue(store, out var cached) && cached.Revision == comp.CatalogRevision)
        {
            var cachedList = cached.List;

            var hasBuy = false;
            var hasSell = false;

            foreach (var l in cachedList)
            {
                if (l.Mode == StoreMode.Buy)
                    hasBuy = true;
                else if (l.Mode == StoreMode.Sell)
                    hasSell = true;

                if (hasBuy && hasSell)
                    break;
            }

            var msg = new StoreCatalogMessage(
                comp.CatalogRevision,
                cachedList,
                hasBuy,
                hasSell,
                comp.ContractPresets.Count > 0
            );
            _ui.ServerSendUiMessage((store, null), StoreUiKey.Key, msg, user);
            return;
        }


        var list = new List<StoreListingStaticData>(comp.Listings.Count);

        foreach (var l in comp.Listings)
        {
            if (string.IsNullOrWhiteSpace(l.Id) || string.IsNullOrWhiteSpace(l.ProductEntity))
                continue;

            var cat = l.Categories.Count > 0 ? l.Categories[0] : Loc.GetString("nc-store-category-fallback");

            if (!TryPickUiCurrencyAndPrice(comp, l, out var cur, out var price))
                continue;

            list.Add(
                new(
                    l.Id,
                    l.Mode,
                    cat,
                    l.ProductEntity,
                    price,
                    cur,
                    l.UnitsPerPurchase
                ));
        }

        _catalogCache[store] = (comp.CatalogRevision, list);

        {
            var hasBuy = false;
            var hasSell = false;

            foreach (var l in list)
            {
                if (l.Mode == StoreMode.Buy)
                    hasBuy = true;
                else if (l.Mode == StoreMode.Sell)
                    hasSell = true;

                if (hasBuy && hasSell)
                    break;
            }

            var msg = new StoreCatalogMessage(
                comp.CatalogRevision,
                list,
                hasBuy,
                hasSell,
                comp.ContractPresets.Count > 0
            );

            _ui.ServerSendUiMessage((store, null), StoreUiKey.Key, msg, user);
        }
    }

    private void OnUiClosed(EntityUid uid, NcStoreComponent comp, BoundUIClosedEvent ev)
    {
        if (!ev.UiKey.Equals(StoreUiKey.Key))
            return;
        comp.CurrentUser = null;
        CloseAndCleanUp(uid);
    }

    private void OnUiRefreshRequest(EntityUid uid, NcStoreComponent comp, RequestUiRefreshMessage msg)
    {
        if (!TryGetLockedUiUser(uid, comp, out var user))
        {
            CloseAndCleanUp(uid);
            return;
        }

        if (!_storeSystem.CanUseStore(uid, comp, user))
        {
            _ui.CloseUi(uid, StoreUiKey.Key, user);
            comp.CurrentUser = null;
            CloseAndCleanUp(uid);
            return;
        }

        if (TryComp(uid, out TransformComponent? sX) && TryComp(user, out TransformComponent? uX) &&
            !_xform.InRange(sX.Coordinates, uX.Coordinates, AutoCloseDistance))
        {
            _ui.CloseUi(uid, StoreUiKey.Key, user);
            comp.CurrentUser = null;
            CloseAndCleanUp(uid);
            return;
        }

        EnsureCrateWatchUpToDate(uid, user);
        UpdateDynamicState(uid, comp, user);
    }

    private void OnAccessReaderChanged(
        EntityUid uid,
        AccessReaderComponent comp,
        ref AccessReaderConfigurationChangedEvent args
    )
    {
        if (TryComp<NcStoreComponent>(uid, out var store) && store.CurrentUser is { } user)
        {
            if (!_storeSystem.CanUseStore(uid, store, user))
            {
                _ui.CloseUi(uid, StoreUiKey.Key, user);
                store.CurrentUser = null;
                CloseAndCleanUp(uid);
            }
        }
    }

    private void OnClaimContract(EntityUid uid, NcStoreComponent comp, ClaimContractBoundMessage msg)
    {
        if (!TryGetLockedUiUser(uid, comp, out var user))
            return;

        if (!_storeSystem.CanUseStore(uid, comp, user))
            return;

        if (TryComp(uid, out TransformComponent? sX) && TryComp(user, out TransformComponent? uX) &&
            !_xform.InRange(sX.Coordinates, uX.Coordinates, AutoCloseDistance))
            return;

        if (_contracts.TryClaim(uid, user, msg.ContractId))
        {
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg"), user);
            _popups.PopupEntity(Loc.GetString("nc-store-contract-completed"), uid, user);
            UpdateDynamicState(uid, comp, user);
            return;
        }

        NcInventorySnapshot? crateSnap = null;
        EntityUid crate = default;
        _logic.TryGetPulledClosedCrate(user, out crate);
        var userSnap = _logic.BuildInventorySnapshot(user);
        if (crate != default)
            crateSnap = _logic.BuildInventorySnapshot(crate);

        UpdateContractsProgress(comp, userSnap, crateSnap);
    }

    private void MarkDirty(EntityUid storeUid)
    {
        if (storeUid != EntityUid.Invalid)
            _dirtyStores.Add(storeUid);
    }

    private bool TryPickUiCurrencyAndPrice(
        NcStoreComponent comp,
        NcStoreListingDef listing,
        out string currencyId,
        out int price
    )
    {
        currencyId = string.Empty;
        price = 0;
        if (listing.Cost.Count == 0)
            return false;
        foreach (var cur in comp.CurrencyWhitelist)
        {
            if (string.IsNullOrWhiteSpace(cur))
                continue;
            if (listing.Cost.TryGetValue(cur, out var p) && p > 0)
            {
                currencyId = cur;
                price = p;
                return true;
            }
        }

        KeyValuePair<string, int>? best = null;
        foreach (var kv in listing.Cost)
        {
            if (string.IsNullOrWhiteSpace(kv.Key) || kv.Value <= 0)
                continue;

            if (best == null || string.CompareOrdinal(kv.Key, best.Value.Key) < 0)
                best = kv;
        }

        if (best == null)
            return false;

        currencyId = best.Value.Key;
        price = best.Value.Value;
        return true;
    }

    private ContractClientData MapContractToClient(ContractServerData c)
    {
        var targets = new List<ContractTargetClientData>();

        if (c.Targets is { Count: > 0, })
        {
            foreach (var t in c.Targets)
            {
                if (string.IsNullOrWhiteSpace(t.TargetItem) || t.Required <= 0)
                    continue;

                targets.Add(
                    new(t.TargetItem, t.Required, t.Progress)
                    {
                        MatchMode = t.MatchMode
                    });
            }
        }
        else if (!string.IsNullOrWhiteSpace(c.TargetItem) && c.Required > 0)
        {
            targets.Add(
                new(c.TargetItem, c.Required, c.Progress)
                {
                    MatchMode = c.MatchMode
                });
        }

        var rewards = c.Rewards is { Count: > 0, }
            ? new(c.Rewards)
            : new List<ContractRewardData>();

        return new(
            c.Id,
            c.Name,
            c.Difficulty,
            c.Description,
            c.Repeatable,
            c.Completed,
            c.TargetItem,
            c.Required,
            c.Progress,
            targets,
            rewards
        );
    }

    private sealed class DynamicScratch
    {
        private readonly DynamicStateBuffer[] _buffers = { new(), new(), };
        private readonly HashSet<string> _visibleListingIds = new();
        private int _activeIndex;
        private int _catalogRevision;
        private bool _hasBuyTab;
        private bool _hasContracts;
        private bool _hasMeta;
        private bool _hasSellTab;
        private bool _hasVisibleIds;
        private int _visibleSig;
        public TimeSpan NextDynamicAllowed = TimeSpan.Zero;


        public DynamicStateBuffer GetReadBuffer() => _buffers[_activeIndex];

        public DynamicStateBuffer GetWriteBuffer() => _buffers[1 - _activeIndex];

        public bool UpdateVisibleIds(string[]? ids)
        {
            if (ids == null || ids.Length == 0)
            {
                if (!_hasVisibleIds)
                    return false;

                _visibleListingIds.Clear();
                _visibleSig = 0;
                _hasVisibleIds = false;
                return true;
            }

            var sig = 17;
            for (var i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                sig = unchecked(sig * 31 + id.GetHashCode());
            }

            if (_hasVisibleIds && sig == _visibleSig && _visibleListingIds.Count == ids.Length)
            {
                var all = true;
                for (var i = 0; i < ids.Length; i++)
                {
                    var id = ids[i];
                    if (string.IsNullOrWhiteSpace(id))
                        continue;
                    if (!_visibleListingIds.Contains(id))
                    {
                        all = false;
                        break;
                    }
                }

                if (all)
                    return false;
            }

            _visibleListingIds.Clear();
            for (var i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                if (!string.IsNullOrWhiteSpace(id))
                    _visibleListingIds.Add(id);
            }

            _visibleSig = sig;
            _hasVisibleIds = true;
            return true;
        }

        public bool ShouldSendBuyDynamicFor(string listingId)
        {
            if (!_hasVisibleIds)
                return true;

            return _visibleListingIds.Contains(listingId);
        }

        public bool EqualsLast(
            DynamicStateBuffer next,
            int catalogRevision,
            bool hasBuyTab,
            bool hasSellTab,
            bool hasContracts
        )
        {
            if (!_hasMeta)
                return false;

            if (_catalogRevision != catalogRevision ||
                _hasBuyTab != hasBuyTab ||
                _hasSellTab != hasSellTab ||
                _hasContracts != hasContracts)
                return false;

            var prev = GetReadBuffer();

            return DictEquals(prev.BalancesByCurrency, next.BalancesByCurrency) &&
                DictEquals(prev.RemainingById, next.RemainingById) &&
                DictEquals(prev.OwnedById, next.OwnedById) &&
                DictEquals(prev.CrateUnitsById, next.CrateUnitsById) &&
                DictEquals(prev.CrateTotals, next.CrateTotals) &&
                ListEquals(prev.Contracts, next.Contracts);
        }

        public void Commit(int catalogRevision, bool hasBuyTab, bool hasSellTab, bool hasContracts)
        {
            _activeIndex = 1 - _activeIndex;

            _catalogRevision = catalogRevision;
            _hasBuyTab = hasBuyTab;
            _hasSellTab = hasSellTab;
            _hasContracts = hasContracts;
            _hasMeta = true;
        }

        private static bool DictEquals(Dictionary<string, int> a, Dictionary<string, int> b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a.Count != b.Count)
                return false;

            foreach (var (k, v) in a)
                if (!b.TryGetValue(k, out var bv) || bv != v)
                    return false;

            return true;
        }

        private static bool ListEquals(List<ContractClientData> a, List<ContractClientData> b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a.Count != b.Count)
                return false;

            for (var i = 0; i < a.Count; i++)
                if (!EqualityComparer<ContractClientData>.Default.Equals(a[i], b[i]))
                    return false;

            return true;
        }
    }

    private sealed class DynamicStateBuffer
    {
        public readonly Dictionary<string, int> BalancesByCurrency = new();
        public readonly List<ContractClientData> Contracts = new();
        public readonly Dictionary<string, int> CrateTotals = new();
        public readonly Dictionary<string, int> CrateUnitsById = new();
        public readonly Dictionary<string, int> OwnedById = new();
        public readonly Dictionary<string, int> RemainingById = new();

        public void Clear()
        {
            BalancesByCurrency.Clear();
            RemainingById.Clear();
            OwnedById.Clear();
            CrateUnitsById.Clear();
            CrateTotals.Clear();
            Contracts.Clear();
        }
    }
}
