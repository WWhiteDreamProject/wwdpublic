using Content.Shared._NC.Trade;
using Content.Shared.Hands.Components;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server._NC.Trade;


public sealed partial class NcStoreLogicSystem
{
    private StoreSpawnService _spawnService = default!;

    private void InitializeServices() => _spawnService = new(this);

    public bool TryPickCurrencyForBuy(
        NcStoreComponent store,
        NcStoreListingDef listing,
        in NcInventorySnapshot snapshot,
        out string currency,
        out int unitPrice,
        out int balance
    ) =>
        _currency.TryPickCurrencyForBuy(store, listing, snapshot, out currency, out unitPrice, out balance);

    public bool TryPickCurrencyForSell(
        NcStoreComponent store,
        NcStoreListingDef listing,
        out string currency,
        out int unitPrice
    ) =>
        _currency.TryPickCurrencyForSell(store, listing, out currency, out unitPrice);

    private bool TryTakeCurrency(EntityUid user, string stackType, int amount) =>
        _currency.TryTakeCurrency(user, stackType, amount);

    public void GiveCurrency(EntityUid user, string stackType, int amount) =>
        _currency.GiveCurrency(user, stackType, amount);


    private sealed class StoreSpawnService
    {
        private readonly string _stackComponentName;
        private readonly NcStoreLogicSystem _sys;
        public StoreSpawnService(NcStoreLogicSystem sys)
        {
            _sys = sys;
            _stackComponentName = _sys._compFactory.GetComponentName(typeof(StackComponent));
        }

        public int SpawnPurchasedProduct(
            EntityUid user,
            string productEntity,
            EntityPrototype productProto,
            int amount,
            int unitPrice,
            string currency
        )
        {
            if (amount <= 0)
                return 0;
            var spawnedTotal = 0;
            if (productProto.TryGetComponent(_stackComponentName, out StackComponent? stackComp))
            {
                var remainingToSpawn = amount;

                var stackTypeId = stackComp.StackTypeId;
                var maxPerStack = int.MaxValue;

                if (!string.IsNullOrWhiteSpace(stackTypeId) &&
                    _sys._protos.TryIndex<StackPrototype>(stackTypeId, out var stackTypeProto))
                    maxPerStack = stackTypeProto.MaxCount ?? int.MaxValue;
                if (maxPerStack <= 0)
                    maxPerStack = 1;

                var cachedItems = _sys._inventory.GetOrBuildDeepItemsCacheCompacted(user);

                foreach (var ent in cachedItems)
                {
                    if (remainingToSpawn <= 0)
                        break;

                    if (!_sys._ents.TryGetComponent(ent, out StackComponent? existingStack) ||
                        existingStack.StackTypeId != stackTypeId)
                        continue;

                    var spaceLeft = maxPerStack - existingStack.Count;
                    if (spaceLeft <= 0)
                        continue;

                    var toAdd = Math.Min(spaceLeft, remainingToSpawn);

                    _sys._stacks.SetCount(ent, existingStack.Count + toAdd, existingStack);

                    remainingToSpawn -= toAdd;
                    spawnedTotal += toAdd;
                }

                if (remainingToSpawn <= 0)
                {
                    _sys._inventory.InvalidateInventoryCache(user);
                    return spawnedTotal;
                }

                var userCoords = _sys._ents.GetComponent<TransformComponent>(user).Coordinates;

                while (remainingToSpawn > 0)
                {
                    var chunk = Math.Min(remainingToSpawn, maxPerStack);
                    try
                    {
                        var spawned = _sys._ents.SpawnEntity(productEntity, userCoords);
                        if (_sys._ents.TryGetComponent(spawned, out StackComponent? spawnedStack))
                            _sys._stacks.SetCount(spawned, chunk, spawnedStack);

                        _sys.QueuePickupToHandsOrCrateNextTick(user, spawned);

                        spawnedTotal += chunk;
                        remainingToSpawn -= chunk;
                    }
                    catch (Exception e)
                    {
                        Logger.GetSawmill("ncstore-logic").Error($"Spawn failed during bulk buy: {e}");
                        break;
                    }
                }

                _sys._inventory.InvalidateInventoryCache(user);

                return spawnedTotal;
            }

            for (var i = 0; i < amount; i++)
                if (_sys.TrySpawnProduct(productEntity, user))
                    spawnedTotal++;
                else
                    continue;

            return spawnedTotal;
        }
    }
}
