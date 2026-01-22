using Content.Shared._NC.Trade;
using Content.Shared.Stacks;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;


namespace Content.Server._NC.Trade;


public sealed partial class NcStoreLogicSystem
{
    private const int DefaultMaxStackFallback = 1000;

    public bool TrySpawnProduct(string protoId, EntityUid user) => TrySpawnProductInternal(protoId, user, true);

    private bool TrySpawnProductInternal(string protoId, EntityUid user, bool invalidateCache)
    {
        if (!_protos.HasIndex<EntityPrototype>(protoId))
        {
            Sawmill.Warning($"[NcStore] Prototype not found: {protoId}");
            return false;
        }

        if (!TryComp(user, out TransformComponent? xform))
            return false;

        try
        {
            var spawned = _ents.SpawnEntity(protoId, xform.Coordinates);

            QueuePickupToHandsOrCrateNextTick(user, spawned);

            return true;
        }
        catch (Exception e)
        {
            Sawmill.Error($"[NcStore] Unexpected spawn failure for {protoId}: {e}");
            return false;
        }
    }

    public int TrySpawnProductUnits(string protoId, EntityUid user, int units)
    {
        if (units <= 0 || string.IsNullOrWhiteSpace(protoId) || !Exists(user))
            return 0;

        if (!_protos.TryIndex<EntityPrototype>(protoId, out var productProto))
            return 0;

        if (!TryGetStackInfo(productProto, out var stackTypeId, out var maxPerStack))
            return SpawnNonStackable(protoId, user, units);

        var remaining = units;
        var totalSpawned = 0;

        var added = FillExistingStacks(user, stackTypeId, maxPerStack, remaining);
        remaining -= added;
        totalSpawned += added;

        if (remaining > 0)
        {
            if (TryComp(user, out TransformComponent? xform))
                totalSpawned += SpawnNewStackChunks(xform.Coordinates, user, protoId, remaining, maxPerStack);
        }

        if (totalSpawned > 0)
            _inventory.InvalidateInventoryCache(user);

        return totalSpawned;
    }

    public bool ExecuteContractBatch(Dictionary<(EntityUid Root, string ProtoId), int> plan)
    {
        if (plan.Count == 0)
            return true;

        var grouped = GroupPlanByRoot(plan);

        if (!ValidateBatchRequirements(grouped))
            return false;

        return ProcessBatchExecution(grouped);
    }

    private bool ValidateBatchRequirements(Dictionary<EntityUid, Dictionary<string, int>> grouped)
    {
        foreach (var (root, reqs) in grouped)
        {
            var snap = _inventory.BuildInventorySnapshot(root);
            foreach (var (protoId, totalAmount) in reqs)
            {
                var available = _inventory.GetOwnedFromSnapshot(snap, protoId, PrototypeMatchMode.Exact);
                if (available < totalAmount)
                {
                    Sawmill.Warning(
                        $"[NcStore] Batch validation failed: {ToPrettyString(root)} lacks {protoId} ({available}/{totalAmount}).");
                    return false;
                }
            }
        }

        return true;
    }

    private bool ProcessBatchExecution(Dictionary<EntityUid, Dictionary<string, int>> grouped)
    {
        foreach (var (root, reqs) in grouped)
            if (!ProcessSingleRootExecution(root, reqs))
                return false;
        return true;
    }

    private bool ProcessSingleRootExecution(EntityUid root, Dictionary<string, int> reqs)
    {
        var cachedItems = _inventory.GetOrBuildDeepItemsCacheCompacted(root);
        var mutated = false;

        foreach (var (protoId, totalAmount) in reqs)
        {
            if (totalAmount <= 0)
                continue;

            if (!TryTakeWithRetry(root, ref cachedItems, protoId, totalAmount))
            {
                Sawmill.Error(
                    $"[NcStore] Post-validation take failed: {protoId} x{totalAmount} from {ToPrettyString(root)}. Aborting!");

                if (mutated)
                    _inventory.InvalidateInventoryCache(root);

                return false;
            }

            mutated = true;
        }

        if (mutated)
            _inventory.InvalidateInventoryCache(root);

        return true;
    }

    private bool TryTakeWithRetry(EntityUid root, ref List<EntityUid> cachedItems, string protoId, int amount)
    {
        return _inventory.TryTakeProductUnitsFromCachedList(
            root,
            cachedItems,
            protoId,
            amount,
            PrototypeMatchMode.Exact) || SlowTake(ref cachedItems, root, protoId, amount);
    }

    #region Private Helpers

    private bool TryGetStackInfo(EntityPrototype proto, out string? stackTypeId, out int maxCount)
    {
        stackTypeId = null;
        maxCount = 1;

        if (!proto.TryGetComponent<StackComponent>(out var stackComp, _compFactory))
            return false;

        stackTypeId = stackComp.StackTypeId;

        if (string.IsNullOrWhiteSpace(stackTypeId) ||
            !_protos.TryIndex<StackPrototype>(stackTypeId, out var stackTypeProto))
            return false;

        maxCount = Math.Max(1, stackTypeProto.MaxCount ?? DefaultMaxStackFallback);
        return true;
    }

    private int FillExistingStacks(EntityUid user, string? stackTypeId, int maxCount, int toAdd)
    {
        var remaining = toAdd;
        var addedTotal = 0;
        var cachedItems = _inventory.GetOrBuildDeepItemsCacheCompacted(user);

        foreach (var ent in cachedItems)
        {
            if (remaining <= 0)
                break;
            if (!TryComp(ent, out StackComponent? stack) || stack.StackTypeId != stackTypeId)
                continue;

            var spaceLeft = maxCount - stack.Count;
            if (spaceLeft <= 0)
                continue;

            var amount = Math.Min(spaceLeft, remaining);
            _stacks.SetCount(ent, stack.Count + amount, stack);

            remaining -= amount;
            addedTotal += amount;
        }

        return addedTotal;
    }

    private int SpawnNewStackChunks(
        EntityCoordinates coords,
        EntityUid user,
        string protoId,
        int totalUnits,
        int maxCount
    )
    {
        var remaining = totalUnits;
        var spawnedTotal = 0;

        while (remaining > 0)
        {
            var chunkSize = Math.Min(remaining, maxCount);
            try
            {
                var spawned = _ents.SpawnEntity(protoId, coords);
                if (TryComp(spawned, out StackComponent? stack))
                    _stacks.SetCount(spawned, chunkSize, stack);

                QueuePickupToHandsOrCrateNextTick(user, spawned);

                spawnedTotal += chunkSize;
                remaining -= chunkSize;
            }
            catch (Exception e)
            {
                Sawmill.Error($"[NcStore] Chunk spawn failed: {protoId} x{chunkSize}: {e}");
                break;
            }
        }

        return spawnedTotal;
    }

    private int SpawnNonStackable(string protoId, EntityUid user, int units)
    {
        var count = 0;
        for (var i = 0; i < units; i++)
            if (TrySpawnProductInternal(protoId, user, false))
                count++;

        if (count > 0)
            _inventory.InvalidateInventoryCache(user);

        return count;
    }

    private Dictionary<EntityUid, Dictionary<string, int>> GroupPlanByRoot(
        Dictionary<(EntityUid Root, string ProtoId), int> plan
    )
    {
        var grouped = new Dictionary<EntityUid, Dictionary<string, int>>();

        foreach (var ((root, protoId), amount) in plan)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(protoId))
                continue;

            var items = grouped.GetOrNew(root);
            try
            {
                checked { items[protoId] = items.GetValueOrDefault(protoId) + amount; }
            }
            catch (OverflowException)
            {
                items[protoId] = int.MaxValue;
                Sawmill.Warning($"[NcStore] Overflow in GroupPlanByRoot for {protoId} at {ToPrettyString(root)}");
            }
        }

        return grouped;
    }

    private bool SlowTake(ref List<EntityUid> cachedItems, EntityUid root, string protoId, int amount)
    {
        cachedItems = _inventory.GetOrBuildDeepItemsCacheCompacted(root);
        return _inventory.TryTakeProductUnitsFromCachedList(
            root,
            cachedItems,
            protoId,
            amount,
            PrototypeMatchMode.Exact);
    }

    #endregion
}
