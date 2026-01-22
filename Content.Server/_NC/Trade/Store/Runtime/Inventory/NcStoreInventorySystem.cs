using Content.Shared._NC.Trade;
using Content.Shared.Clothing.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;


namespace Content.Server._NC.Trade;


public sealed class NcStoreInventorySystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IEntityManager _ents = default!;
    private readonly Dictionary<EntityUid, List<EntityUid>> _inventoryCache = new();
    private readonly HashSet<EntityUid> _inventoryDirty = new();
    private readonly Dictionary<string, string?> _productStackTypeCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string[]> _protoAndAncestorsCache = new(StringComparer.Ordinal);
    private readonly HashSet<string> _scratchProtoVisited = new(StringComparer.Ordinal);
    private readonly List<string> _scratchProtoResult = new();
    private readonly List<string> _scratchProtoStack = new();
    [Dependency] private readonly IPrototypeManager _protos = default!;
    private readonly Queue<EntityUid> _scratchQueue = new();
    private readonly List<EntityUid> _scratchResult = new();
    private readonly HashSet<EntityUid> _scratchVisited = new();
    [Dependency] private readonly SharedStackSystem _stacks = default!;

    public override void Initialize()
    {
        base.Initialize();
        _protos.PrototypesReloaded += OnPrototypesReloaded;
        SubscribeLocalEvent<EntityTerminatingEvent>(OnEntityTerminating);
    }

    public override void Shutdown()
    {
        _protos.PrototypesReloaded -= OnPrototypesReloaded;
        base.Shutdown();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        _productStackTypeCache.Clear();
        _protoAndAncestorsCache.Clear();
        InvalidateAllCaches();
    }

    private void OnEntityTerminating(ref EntityTerminatingEvent ev)
    {
        _inventoryCache.Remove(ev.Entity);
        _inventoryDirty.Remove(ev.Entity);
    }


    public void InvalidateInventoryCache(EntityUid root)
    {
        // Keep the list instance to reduce GC; mark it dirty for rebuild.
        if (!_inventoryCache.ContainsKey(root))
            _inventoryCache[root] = new List<EntityUid>();
        _inventoryDirty.Add(root);
    }

    public void InvalidateAllCaches()
    {
        _inventoryDirty.Clear();
        _inventoryCache.Clear();
    }

    public List<EntityUid> GetOrBuildDeepItemsCache(EntityUid owner)
    {
        if (_inventoryCache.TryGetValue(owner, out var cached))
        {
            if (_inventoryDirty.Remove(owner))
                BuildDeepItemsCache(owner, cached);
            return cached;
        }

        cached = new List<EntityUid>();
        _inventoryCache[owner] = cached;
        BuildDeepItemsCache(owner, cached);
        return cached;
    }

    public List<EntityUid> GetOrBuildDeepItemsCacheCompacted(EntityUid owner)
    {
        var cached = GetOrBuildDeepItemsCache(owner);
        CompactCachedItemsIfNeeded(cached);
        return cached;
    }


    private void BuildDeepItemsCache(EntityUid owner, List<EntityUid> cached)
    {
        _scratchVisited.Clear();
        _scratchQueue.Clear();
        _scratchResult.Clear();

        void Enqueue(EntityUid uid)
        {
            if (uid == EntityUid.Invalid)
                return;
            if (!_scratchVisited.Add(uid))
                return;
            _scratchQueue.Enqueue(uid);
            _scratchResult.Add(uid);
        }

        if (_ents.TryGetComponent(owner, out InventoryComponent? inventory))
        {
            var slotEnum = new InventorySystem.InventorySlotEnumerator(inventory);
            while (slotEnum.NextItem(out var item))
                Enqueue(item);
        }

        if (_ents.TryGetComponent(owner, out ItemSlotsComponent? itemSlots))
        {
            foreach (var slot in itemSlots.Slots.Values)
                if (slot is { HasItem: true, Item: not null })
                    Enqueue(slot.Item.Value);
        }

        if (_ents.TryGetComponent(owner, out HandsComponent? hands))
        {
            foreach (var hand in hands.Hands.Values)
                if (hand.HeldEntity.HasValue)
                    Enqueue(hand.HeldEntity.Value);
        }

        if (_ents.TryGetComponent(owner, out ContainerManagerComponent? cmcRoot))
        {
            foreach (var container in cmcRoot.Containers.Values)
                foreach (var entity in container.ContainedEntities)
                    Enqueue(entity);
        }

        while (_scratchQueue.Count > 0)
        {
            var current = _scratchQueue.Dequeue();
            if (!_ents.TryGetComponent(current, out ContainerManagerComponent? cmc))
                continue;

            foreach (var container in cmc.Containers.Values)
                foreach (var child in container.ContainedEntities)
                    Enqueue(child);
        }

        cached.Clear();
        if (cached.Capacity < _scratchResult.Count)
            cached.Capacity = _scratchResult.Count;
        cached.AddRange(_scratchResult);
        }

    private void CompactCachedItems(List<EntityUid> cached)
    {
        var w = 0;
        for (var r = 0; r < cached.Count; r++)
        {
            var ent = cached[r];
            if (ent != EntityUid.Invalid && _ents.EntityExists(ent))
                cached[w++] = ent;
        }

        if (w < cached.Count)
            cached.RemoveRange(w, cached.Count - w);
    }



    private void CompactCachedItemsIfNeeded(List<EntityUid> cached)
    {
        if (cached.Count < 256)
            return;

        var invalid = 0;
        var threshold = Math.Max(64, cached.Count / 4);

        // Fast detection: stop as soon as we know we must compact.
        for (var i = 0; i < cached.Count; i++)
        {
            var ent = cached[i];
            if (ent == EntityUid.Invalid || !_ents.EntityExists(ent))
            {
                invalid++;
                if (invalid >= threshold)
                    break;
            }
        }

        if (invalid < threshold)
            return;

        CompactCachedItems(cached);
    }


public NcInventorySnapshot BuildInventorySnapshot(EntityUid root)
    {
        var snap = new NcInventorySnapshot();
        FillInventorySnapshot(root, snap);
        return snap;
    }

    public void FillInventorySnapshot(EntityUid root, NcInventorySnapshot buffer)
    {
        var items = GetOrBuildDeepItemsCache(root);
        FillInventorySnapshotFromItems(root, items, buffer);
    }

    public void ScanInventory(EntityUid root, List<EntityUid> itemsBuffer, NcInventorySnapshot snapshotBuffer)
    {
        itemsBuffer.Clear();
        var cached = GetOrBuildDeepItemsCacheCompacted(root);
        itemsBuffer.AddRange(cached);
        FillInventorySnapshotFromItems(root, itemsBuffer, snapshotBuffer);
    }


public void ScanInventoryItems(EntityUid root, List<EntityUid> itemsBuffer)
{
    itemsBuffer.Clear();
    var cached = GetOrBuildDeepItemsCacheCompacted(root);
    itemsBuffer.AddRange(cached);
}


    private void FillInventorySnapshotFromItems(
        EntityUid root,
        IReadOnlyList<EntityUid> items,
        NcInventorySnapshot buffer
    )
    {
        buffer.Clear();
        foreach (var ent in items)
        {
            if (!_ents.EntityExists(ent))
                continue;
            if (IsProtectedFromDirectSale(root, ent))
                continue;

            _ents.TryGetComponent(ent, out MetaDataComponent? meta);
            var proto = meta?.EntityPrototype;

            if (_ents.TryGetComponent(ent, out StackComponent? stack))
            {
                var cnt = Math.Max(stack.Count, 0);
                if (cnt > 0 && !string.IsNullOrWhiteSpace(stack.StackTypeId))
                {
                    buffer.StackTypeCounts.TryGetValue(stack.StackTypeId, out var prev);
                    buffer.StackTypeCounts[stack.StackTypeId] = prev + cnt;
                }

                if (cnt > 0 && proto != null)
                {
                    if (!buffer.ProtoCounts.TryAdd(proto.ID, cnt))
                        buffer.ProtoCounts[proto.ID] += cnt;

                    foreach (var id in GetProtoAndAncestors(proto))
                    {
                        buffer.AncestorCounts.TryGetValue(id, out var prev);
                        buffer.AncestorCounts[id] = prev + cnt;
                    }
                }

                continue;
            }

            if (proto == null)
                continue;

            if (!buffer.ProtoCounts.TryAdd(proto.ID, 1))
                buffer.ProtoCounts[proto.ID] += 1;

            foreach (var id in GetProtoAndAncestors(proto))
            {
                buffer.AncestorCounts.TryGetValue(id, out var prev);
                buffer.AncestorCounts[id] = prev + 1;
            }
        }
    }

    public int GetOwnedFromSnapshot(
        in NcInventorySnapshot snapshot,
        string productProtoId,
        PrototypeMatchMode matchMode
    )
    {
        var stackType = GetProductStackType(productProtoId);
        if (stackType != null)
            return snapshot.StackTypeCounts.TryGetValue(stackType, out var cnt) ? cnt : 0;

        var effective = ResolveMatchMode(productProtoId, matchMode);
        if (effective == PrototypeMatchMode.Descendants)
            return snapshot.AncestorCounts.TryGetValue(productProtoId, out var units) ? units : 0;

        return snapshot.ProtoCounts.TryGetValue(productProtoId, out var exact) ? exact : 0;
    }


    public bool TryTakeProductUnitsFromRootCached(
        EntityUid root,
        string protoId,
        int amount,
        PrototypeMatchMode matchMode
    )
    {
        if (amount <= 0)
            return true;
        var cachedItems = GetOrBuildDeepItemsCache(root);
        return TryTakeProductUnitsFromCachedList(root, cachedItems, protoId, amount, matchMode);
    }

    public bool TryTakeProductUnitsFromCachedList(
        EntityUid root,
        List<EntityUid> cachedItems,
        string protoId,
        int amount,
        PrototypeMatchMode matchMode
    )
    {
        if (amount <= 0)
            return true;

        var stackType = GetProductStackType(protoId);
        var effective = ResolveMatchMode(protoId, matchMode);
        var availableTotal = 0;

        bool Matches(EntityPrototype proto)
        {
            if (effective == PrototypeMatchMode.Exact)
                return proto.ID == protoId;
            return proto.ID == protoId || IsProtoOrDescendant(proto, protoId);
        }

        foreach (var ent in cachedItems)
        {
            if (ent == EntityUid.Invalid || !_ents.EntityExists(ent))
                continue;
            if (IsProtectedFromDirectSale(root, ent))
                continue;

            if (stackType != null)
            {
                if (_ents.TryGetComponent(ent, out StackComponent? stack) && stack.StackTypeId == stackType)
                    availableTotal += Math.Max(stack.Count, 0);
            }
            else
            {
                if (_ents.TryGetComponent(ent, out MetaDataComponent? meta) && meta.EntityPrototype != null)
                {
                    if (Matches(meta.EntityPrototype))
                    {
                        if (_ents.TryGetComponent(ent, out StackComponent? st) && st.Count > 0)
                            availableTotal += st.Count;
                        else
                            availableTotal += 1;
                    }
                }
            }

            if (availableTotal >= amount)
                break;
        }

        if (availableTotal < amount)
            return false;

        var left = amount;
        var compactNeeded = false;

        for (var i = 0; i < cachedItems.Count && left > 0; i++)
        {
            var ent = cachedItems[i];
            if (ent == EntityUid.Invalid || !_ents.EntityExists(ent))
                continue;
            if (IsProtectedFromDirectSale(root, ent))
                continue;

            if (stackType != null)
            {
                if (!_ents.TryGetComponent(ent, out StackComponent? stack) || stack.StackTypeId != stackType)
                    continue;

                var have = Math.Max(stack.Count, 0);
                if (have <= 0)
                    continue;

                var take = Math.Min(have, left);
                _stacks.SetCount(ent, have - take, stack);

                if (stack.Count <= 0)
                {
                    _ents.DeleteEntity(ent);
                    cachedItems[i] = EntityUid.Invalid;
                compactNeeded = true;
                }

                left -= take;
                continue;
            }

            if (!_ents.TryGetComponent(ent, out MetaDataComponent? meta) || meta.EntityPrototype == null)
                continue;

            if (meta.EntityPrototype.ID == protoId)
            {
                ProcessTake(i, ent);
                continue;
            }

            if (Matches(meta.EntityPrototype))
                ProcessTake(i, ent);
        }

        void ProcessTake(int index, EntityUid item)
        {
            if (left <= 0)
                return;

            if (_ents.TryGetComponent(item, out StackComponent? st))
            {
                var have = Math.Max(st.Count, 0);
                var take = Math.Min(have, left);
                _stacks.SetCount(item, have - take, st);

                if (st.Count <= 0)
                {
                    _ents.DeleteEntity(item);
                    cachedItems[index] = EntityUid.Invalid;
                compactNeeded = true;
                }

                left -= take;
            }
            else
            {
                _ents.DeleteEntity(item);
                cachedItems[index] = EntityUid.Invalid;
                compactNeeded = true;
                left -= 1;
            }
        }

        if (compactNeeded)
            CompactCachedItemsIfNeeded(cachedItems);

        return left <= 0;
    }


    public bool IsProtectedFromDirectSale(EntityUid root, EntityUid item)
    {
        if (!_ents.HasComponent<InventoryComponent>(root))
            return false;

        if (!IsDirectChildOf(root, item))
            return false;
        if (IsHeldInHands(root, item))
            return false;

        return _ents.HasComponent<ClothingComponent>(item);
    }

    private bool IsDirectChildOf(EntityUid root, EntityUid item) =>
        _ents.TryGetComponent(item, out TransformComponent? xform) && xform.ParentUid == root;

    private bool IsHeldInHands(EntityUid user, EntityUid item)
    {
        if (!_ents.TryGetComponent(user, out HandsComponent? hands))
            return false;
        foreach (var hand in hands.Hands.Values)
            if (hand.HeldEntity == item)
                return true;
        return false;
    }

    public string? GetProductStackType(string productProtoId)
    {
        if (_productStackTypeCache.TryGetValue(productProtoId, out var cached))
            return cached;

        string? stackType = null;
        if (_protos.TryIndex<EntityPrototype>(productProtoId, out var proto))
        {
            var stackName = _compFactory.GetComponentName(typeof(StackComponent));
            if (proto.TryGetComponent(stackName, out StackComponent? prodStackDef))
                stackType = prodStackDef.StackTypeId;
        }

        _productStackTypeCache[productProtoId] = stackType;
        return stackType;
    }

    public PrototypeMatchMode ResolveMatchMode(string expectedProtoId, PrototypeMatchMode configured)
    {
        if (configured == PrototypeMatchMode.Descendants)
            return PrototypeMatchMode.Descendants;
        if (_protos.TryIndex<EntityPrototype>(expectedProtoId, out var p) && p.Abstract)
            return PrototypeMatchMode.Descendants;
        return PrototypeMatchMode.Exact;
    }

    public string[] GetProtoAndAncestors(EntityPrototype proto)
    {
        if (_protoAndAncestorsCache.TryGetValue(proto.ID, out var cached))
            return cached;

        _scratchProtoVisited.Clear();
        _scratchProtoResult.Clear();
        _scratchProtoStack.Clear();

        _scratchProtoStack.Add(proto.ID);

        while (_scratchProtoStack.Count > 0)
        {
            var idx = _scratchProtoStack.Count - 1;
            var cur = _scratchProtoStack[idx];
            _scratchProtoStack.RemoveAt(idx);

            if (!_scratchProtoVisited.Add(cur))
                continue;

            _scratchProtoResult.Add(cur);

            if (_protos.TryIndex<EntityPrototype>(cur, out var curProto) && curProto.Parents != null)
            {
                foreach (var p in curProto.Parents)
                {
                    if (!string.IsNullOrWhiteSpace(p))
                        _scratchProtoStack.Add(p);
                }
            }
        }

        var arr = _scratchProtoResult.ToArray();
        _protoAndAncestorsCache[proto.ID] = arr;
        return arr;
    }

    private bool IsProtoOrDescendant(EntityPrototype candidate, string expectedId)
    {
        if (candidate.ID == expectedId)
            return true;
        var ancestors = GetProtoAndAncestors(candidate);
        foreach (var t in ancestors)
            if (t == expectedId)
                return true;
        return false;
    }
}
