using Content.Shared._NC.Trade;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server._NC.Trade;

public sealed class NcStoreCurrencySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protos = default!;

    [Dependency] private readonly IEntityManager _ents = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly NcStoreInventorySystem _inventory = default!;
    [Dependency] private readonly SharedStackSystem _stacks = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private readonly List<ICurrencyHandler> _handlers = new();
    private readonly Dictionary<string, ICurrencyHandler> _handlerCache = new(StringComparer.Ordinal);

    public override void Initialize()
    {
        base.Initialize();
        _handlers.Clear();
        _handlerCache.Clear();
        _handlers.Add(new StackCurrencyHandler(_ents, _hands, _inventory, _protos, _stacks, _xform));
    }

    private bool TryResolveHandler(string currencyId, out ICurrencyHandler handler)
    {
        if (_handlerCache.TryGetValue(currencyId, out var cached))
        {
            handler = cached;
            return true;
        }

        foreach (var h in _handlers)
        {
            if (!h.CanHandle(currencyId))
                continue;

            _handlerCache[currencyId] = h;
            handler = h;
            return true;
        }

        handler = default!;
        return false;
    }


    public bool TryGetBalance(in NcInventorySnapshot snapshot, string currencyId, out int balance)
    {
        balance = 0;
        if (!TryResolveHandler(currencyId, out var h))
            return false;
        return h.TryGetBalance(snapshot, currencyId, out balance);
    }

    public bool TryPickCurrencyForBuy(
        NcStoreComponent store,
        NcStoreListingDef listing,
        in NcInventorySnapshot snapshot,
        out string currency,
        out int unitPrice,
        out int balance
    )
    {
        currency = string.Empty;
        unitPrice = 0;
        balance = 0;

        if (listing.Cost.Count == 0)
            return false;

        var hasWhitelist = false;
        foreach (var c in store.CurrencyWhitelist)
            if (!string.IsNullOrWhiteSpace(c))
            {
                hasWhitelist = true;
                break;
            }

        if (hasWhitelist)
        {
            foreach (var cur in store.CurrencyWhitelist)
            {
                if (string.IsNullOrWhiteSpace(cur))
                    continue;
                if (!listing.Cost.TryGetValue(cur, out var price))
                    continue;
                if (price <= 0)
                    continue;

                if (!TryGetBalance(snapshot, cur, out var bal))
                    bal = 0;
                if (bal < price)
                    continue;

                currency = cur;
                unitPrice = price;
                balance = bal;
                return true;
            }

            return false;
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

        var fallbackCur = best.Value.Key;
        var fallbackPrice = best.Value.Value;
        if (!TryGetBalance(snapshot, fallbackCur, out var fallbackBal))
            fallbackBal = 0;

        if (fallbackBal < fallbackPrice)
            return false;

        currency = fallbackCur;
        unitPrice = fallbackPrice;
        balance = fallbackBal;
        return true;
    }

    public bool TryPickCurrencyForSell(
        NcStoreComponent store,
        NcStoreListingDef listing,
        out string currency,
        out int unitPrice
    )
    {
        currency = string.Empty;
        unitPrice = 0;
        if (listing.Cost.Count == 0)
            return false;

        foreach (var cur in store.CurrencyWhitelist)
        {
            if (string.IsNullOrWhiteSpace(cur))
                continue;
            if (listing.Cost.TryGetValue(cur, out var price) && price > 0 && TryResolveHandler(cur, out _))
            {
                currency = cur;
                unitPrice = price;
                return true;
            }
        }

        KeyValuePair<string, int>? best = null;
        foreach (var kv in listing.Cost)
        {
            if (string.IsNullOrWhiteSpace(kv.Key) || kv.Value <= 0)
                continue;

            if (!TryResolveHandler(kv.Key, out _))
                continue;

            if (best == null || string.CompareOrdinal(kv.Key, best.Value.Key) < 0)
                best = kv;
        }

        if (best == null)
            return false;

        currency = best.Value.Key;
        unitPrice = best.Value.Value;
        return true;
    }

    public bool TryTakeCurrency(EntityUid user, string currencyId, int amount)
    {
        if (amount <= 0)
            return true;
        if (!TryResolveHandler(currencyId, out var h))
            return false;
        return h.TryTake(user, currencyId, amount);
    }

    public void GiveCurrency(EntityUid user, string currencyId, int amount)
    {
        if (amount <= 0)
            return;
        if (!TryResolveHandler(currencyId, out var h))
            return;
        h.TryGiveCurrency(user, currencyId, amount);
    }
}
