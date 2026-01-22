using Content.Shared._NC.Trade;
using Robust.Shared.Random;


namespace Content.Server._NC.Trade;


public sealed partial class NcContractSystem : EntitySystem
{
    private List<ContractRewardData> BakeRewardsForContract(EntityUid store, StoreContractPrototype proto)
    {
        if (proto.Rewards.Count == 0)
            return new();

        var baked = BakeRewardsRecursive(store, proto.ID, proto.Rewards, 0);
        return AggregateRewards(baked);
    }

    private List<ContractRewardData> BakeRewardsRecursive(
        EntityUid store,
        string contractProtoId,
        List<ContractRewardDef> blueprints,
        int depth
    )
    {
        var result = new List<ContractRewardData>();
        if (depth > MaxRewardDepth)
            return result;

        for (var i = 0; i < blueprints.Count; i++)
        {
            var bp = blueprints[i];

            if (bp.Probability < 1.0f && !_random.Prob(Math.Clamp(bp.Probability, 0f, 1f)))
                continue;
            var count = RollFair(
                new(QuasiKeyKind.RAmount, store, contractProtoId, $"{depth}:{i}:{bp.Type}:{bp.Id}"),
                bp.Amount,
                0);

            if (count <= 0)
                continue;

            var isPool = bp.Type == StoreRewardType.Pool || bp.Options is { Count: > 0 };

            if (isPool)
            {
                var rolled = RollPool(store, contractProtoId, bp, count, depth + 1);
                result.AddRange(rolled);
                continue;
            }

            if (string.IsNullOrWhiteSpace(bp.Id))
                continue;

            if (bp.Type != StoreRewardType.Item && bp.Type != StoreRewardType.Currency)
                continue;

            result.Add(new(bp.Type, bp.Id, count));
        }

        return result;
    }

    private List<ContractRewardData> RollPool(
        EntityUid store,
        string contractProtoId,
        ContractRewardDef poolDef,
        int rolls,
        int depth
    )
    {
        var output = new List<ContractRewardData>();
        if (depth > MaxRewardDepth)
            return output;

        List<ContractRewardDef>? options = null;
        if (poolDef.Options is { Count: > 0 })
            options = poolDef.Options;
        else if (!string.IsNullOrWhiteSpace(poolDef.Id) &&
            _prototypes.TryIndex<NcContractRewardPoolPrototype>(poolDef.Id, out var poolProto) &&
            poolProto.Entries is { Count: > 0 })
            options = poolProto.Entries;

        if (options == null || options.Count == 0)
            return output;

        var deck = new List<PoolEntry>(options.Count);
        for (var i = 0; i < options.Count; i++)
        {
            var def = options[i];
            var key = $"{i}:{def.Type}:{def.Id}";
            deck.Add(new(def, key));
        }

        var dropCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var i = 0; i < rolls; i++)
        {
            if (deck.Count == 0)
                break;

            var winner = PickWeighted(_random, deck, x => x.Def.Weight);
            var key = winner.Key;

            if (!dropCounts.TryAdd(key, 1))
                dropCounts[key] = dropCounts[key] + 1;

            if (winner.Def.MaxRepeats > 0 && dropCounts[key] >= winner.Def.MaxRepeats)
                deck.Remove(winner);

            output.AddRange(BakeRewardsRecursive(store, contractProtoId, new() { winner.Def }, depth));
        }

        return output;
    }

    private static List<ContractRewardData> AggregateRewards(List<ContractRewardData> rewards)
    {
        if (rewards.Count == 0)
            return rewards;

        var map = new Dictionary<(StoreRewardType Type, string Id), int>();

        foreach (var r in rewards)
        {
            if (r.Amount <= 0 || string.IsNullOrWhiteSpace(r.Id))
                continue;
            if (r.Type != StoreRewardType.Item && r.Type != StoreRewardType.Currency)
                continue;

            var k = (r.Type, r.Id);
            if (!map.TryAdd(k, r.Amount))
                map[k] = checked(map[k] + r.Amount);
        }

        var outList = new List<ContractRewardData>(map.Count);
        foreach (var (k, amt) in map)
        {
            if (amt <= 0)
                continue;
            outList.Add(new(k.Type, k.Id, amt));
        }

        return outList;
    }
}
