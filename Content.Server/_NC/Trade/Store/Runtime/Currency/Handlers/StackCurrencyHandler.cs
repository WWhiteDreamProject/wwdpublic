using Content.Shared.Stacks;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server._NC.Trade;

/// <summary>
/// Stack-based currency implementation.
/// Currency id is interpreted as <see cref="StackComponent.StackTypeId"/> / <see cref="StackPrototype"/> id.
/// </summary>
public sealed class StackCurrencyHandler : ICurrencyHandler
{
    private readonly IEntityManager _ents;
    private readonly SharedHandsSystem _hands;
    private readonly NcStoreInventorySystem _inventory;
    private readonly IPrototypeManager _protos;
    private readonly SharedStackSystem _stacks;
    private readonly SharedTransformSystem _xform;
    private readonly List<(EntityUid Ent, int Count)> _scratchCandidates = new();

    public StackCurrencyHandler(
        IEntityManager ents,
        SharedHandsSystem hands,
        NcStoreInventorySystem inventory,
        IPrototypeManager protos,
        SharedStackSystem stacks,
        SharedTransformSystem xform)
    {
        _ents = ents;
        _hands = hands;
        _inventory = inventory;
        _protos = protos;
        _stacks = stacks;
        _xform = xform;
    }

    public bool CanHandle(string currencyId)
    {
        if (string.IsNullOrWhiteSpace(currencyId))
            return false;

        // StackType ids are stack prototype ids.
        return _protos.HasIndex<StackPrototype>(currencyId);
    }

    public bool TryGetBalance(in NcInventorySnapshot snapshot, string currencyId, out int balance)
    {
        if (string.IsNullOrWhiteSpace(currencyId))
        {
            balance = 0;
            return false;
        }

        balance = snapshot.StackTypeCounts.TryGetValue(currencyId, out var b) ? b : 0;
        return true;
    }

    public bool TryTake(EntityUid user, string currencyId, int amount)
    {
        if (amount <= 0)
            return true;
        if (!CanHandle(currencyId))
            return false;

        var cachedItems = _inventory.GetOrBuildDeepItemsCache(user);

        _scratchCandidates.Clear();
        var total = 0;

        foreach (var ent in cachedItems)
        {
            if (ent == EntityUid.Invalid || !_ents.EntityExists(ent))
                continue;
            if (_inventory.IsProtectedFromDirectSale(user, ent))
                continue;

            if (!_ents.TryGetComponent(ent, out StackComponent? st) || st.StackTypeId != currencyId)
                continue;

            var cnt = Math.Max(st.Count, 0);
            if (cnt <= 0)
                continue;

            _scratchCandidates.Add((ent, cnt));
            total += cnt;
        }

        if (total < amount)
            return false;

        _scratchCandidates.Sort((a, b) => a.Count.CompareTo(b.Count));

        var left = amount;
        foreach (var (ent, have) in _scratchCandidates)
        {
            if (left <= 0)
                break;
            var take = Math.Min(have, left);

            if (_ents.TryGetComponent(ent, out StackComponent? st))
            {
                _stacks.SetCount(ent, st.Count - take, st);
                if (st.Count <= 0)
                    _ents.DeleteEntity(ent);
            }

            left -= take;
        }

        if (left <= 0)
        {
            _inventory.InvalidateInventoryCache(user);
            return true;
        }

        return false;
    }

    public bool TryGiveCurrency(EntityUid user, string currencyId, int amount)
    {
        if (amount <= 0 || string.IsNullOrWhiteSpace(currencyId))
            return true; // Nothing to give, operation is trivially successful.
        if (!_protos.TryIndex<StackPrototype>(currencyId, out var proto))
            return false;

        _inventory.InvalidateInventoryCache(user);

        var maxPerStack = proto.MaxCount ?? int.MaxValue;
        if (maxPerStack <= 0)
            maxPerStack = 1;

        long remaining = amount;

        var cached = _inventory.GetOrBuildDeepItemsCacheCompacted(user);
        foreach (var ent in cached)
        {
            if (remaining <= 0)
                break;
            if (!_ents.TryGetComponent(ent, out StackComponent? st) || st.StackTypeId != currencyId)
                continue;

            var canAdd = (long) maxPerStack - st.Count;
            if (canAdd <= 0)
                continue;

            var add = Math.Min(canAdd, remaining);
            var newCount = (int) Math.Clamp(st.Count + add, 0L, maxPerStack);

            _stacks.SetCount(ent, newCount, st);
            remaining -= add;
        }

        if (remaining <= 0)
        {
            _inventory.InvalidateInventoryCache(user);
            return true;
        }

        var coords = _xform.GetMoverCoordinates(user);

        while (remaining > 0)
        {
            var addL = Math.Min(remaining, maxPerStack);
            var add = (int) Math.Clamp(addL, 1L, maxPerStack);

            var spawned = _ents.SpawnEntity(proto.Spawn, coords);

            if (_ents.TryGetComponent(spawned, out StackComponent? newStack))
                _stacks.SetCount(spawned, add, newStack);

            _hands.TryPickupAnyHand(user, spawned, false);
            remaining -= add;
        }

        _inventory.InvalidateInventoryCache(user);
        return true;
    }
}
