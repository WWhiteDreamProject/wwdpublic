using System.Runtime.InteropServices;
using Content.Shared._NC.Trade;


namespace Content.Server._NC.Trade;


public sealed partial class NcContractSystem : EntitySystem
{
    public void RefillContractsForStore(EntityUid uid, NcStoreComponent comp, string? ignoredContractId = null)
    {
        if (comp.ContractPresets.Count == 0)
            return;

        var presetId = comp.ContractPresets[0];
        if (string.IsNullOrWhiteSpace(presetId))
            return;

        if (!_prototypes.TryIndex<StoreContractsPresetPrototype>(presetId, out var preset))
        {
            Sawmill.Warning($"[Contracts] Preset '{presetId}' not found for {ToPrettyString(uid)}");
            return;
        }

        var currentCounts = CountCurrentContracts(comp);
        var poolByDifficulty = BuildCandidatePool(preset, comp, ignoredContractId);

        foreach (var (difficulty, limit) in preset.Limits)
            ProcessDifficulty(uid, comp, difficulty, limit, currentCounts, poolByDifficulty);
    }

    private void ProcessDifficulty(
        EntityUid uid,
        NcStoreComponent comp,
        string difficulty,
        int limit,
        Dictionary<string, int> currentCounts,
        Dictionary<string, List<(StoreContractPrototype Proto, int Weight)>> poolByDifficulty
    )
    {
        var current = currentCounts.GetValueOrDefault(difficulty, 0);
        var needed = limit - current;

        if (needed <= 0)
            return;

        if (!poolByDifficulty.TryGetValue(difficulty, out var pool) || pool.Count == 0)
            return;

        var cooldownLimit = ComputeEffectiveContractCooldown(pool.Count, needed);

        var cd = GetCooldownState(uid, difficulty);
        cd.Limit = cooldownLimit;
        cd.TrimToLimit();

        List<(StoreContractPrototype Proto, int Weight)> fresh;
        List<(StoreContractPrototype Proto, int Weight)>? recent = null;

        if (cooldownLimit > 0)
        {
            fresh = new(pool.Count);
            recent = new(pool.Count);

            foreach (var e in pool)
                if (cd.Contains(e.Proto.ID))
                    recent.Add(e);
                else
                    fresh.Add(e);
        }
        else
            fresh = pool;

        for (var i = 0; i < needed; i++)
        {
            var source = fresh.Count > 0 ? fresh : recent;
            if (source == null || source.Count == 0)
                break;

            if (!TryPickAndRemoveWeighted(source, out var pick))
                break;

            comp.Contracts[pick.Proto.ID] = CreateContractData(uid, pick.Proto);
            cd.Push(pick.Proto.ID);
        }
    }

    private Dictionary<string, List<(StoreContractPrototype Proto, int Weight)>> BuildCandidatePool(
        StoreContractsPresetPrototype preset,
        NcStoreComponent comp,
        string? ignoredContractId
    )
    {
        var raw = new List<(StoreContractPrototype Proto, int Weight)>();
        var visitedPacks = new HashSet<string>(StringComparer.Ordinal);

        foreach (var packEntry in preset.Packs)
            CollectFromPackRecursive(packEntry.Id, packEntry.Weight, raw, visitedPacks);

        var unique = new Dictionary<string, (StoreContractPrototype Proto, int Weight)>(StringComparer.Ordinal);

        foreach (var (proto, weight) in raw)
        {
            if (weight <= 0)
                continue;

            if (ignoredContractId != null && proto.ID == ignoredContractId)
                continue;

            if (comp.Contracts.ContainsKey(proto.ID))
                continue;

            if (!proto.Repeatable && comp.CompletedOneTimeContracts.Contains(proto.ID))
                continue;

            ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(unique, proto.ID, out var exists);
            if (exists)
                slot.Weight = checked(slot.Weight + weight);
            else
                slot = (proto, weight);
        }

        var result = new Dictionary<string, List<(StoreContractPrototype Proto, int Weight)>>(StringComparer.Ordinal);

        foreach (var entry in unique.Values)
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(
                result,
                entry.Proto.Difficulty,
                out var exists);
            if (!exists)
                list = new();

            list!.Add(entry);
        }

        return result;
    }

    private static Dictionary<string, int> CountCurrentContracts(NcStoreComponent comp)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var c in comp.Contracts.Values)
        {
            ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(counts, c.Difficulty, out _);
            count++;
        }

        return counts;
    }

    private CooldownState GetCooldownState(EntityUid store, string difficulty)
    {
        ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _contractCooldown,
            (store, difficulty),
            out var exists);
        if (!exists)
            state = new();

        return state!;
    }

    private void CollectFromPackRecursive(
        string packId,
        int weightMult,
        List<(StoreContractPrototype Proto, int FinalWeight)> acc,
        HashSet<string> visitedPacks
    )
    {
        if (!visitedPacks.Add(packId))
            return;

        if (!_prototypes.TryIndex<StoreContractPackPrototype>(packId, out var pack))
        {
            Sawmill.Error($"[Contracts] Pack '{packId}' not found.");
            return;
        }

        foreach (var entry in pack.Contracts)
        {
            if (!_prototypes.TryIndex<StoreContractPrototype>(entry.Id, out var proto))
                continue;

            var finalWeight = checked(entry.Weight * weightMult);
            if (finalWeight > 0)
                acc.Add((proto, finalWeight));
        }

        foreach (var include in pack.Includes)
            CollectFromPackRecursive(
                include.Id,
                checked(weightMult * include.Weight),
                acc,
                visitedPacks);
    }

    private static int ComputeEffectiveContractCooldown(int poolCount, int needed)
    {
        if (poolCount <= 1 || needed <= 0)
            return 0;

        var upper = Math.Min(poolCount - 1, poolCount - needed);
        return Math.Max(0, upper);
    }

    private bool TryPickAndRemoveWeighted(
        List<(StoreContractPrototype Proto, int Weight)> list,
        out (StoreContractPrototype Proto, int Weight) picked
    )
    {
        picked = default;

        var total = 0;
        for (var i = 0; i < list.Count; i++)
        {
            var w = list[i].Weight;
            if (w > 0)
                total = checked(total + w);
        }

        if (total <= 0)
            return false;

        var roll = _random.Next(total);

        for (var i = 0; i < list.Count; i++)
        {
            var w = list[i].Weight;
            if (w <= 0)
                continue;

            roll -= w;
            if (roll >= 0)
                continue;

            picked = list[i];

            var last = list.Count - 1;
            list[i] = list[last];
            list.RemoveAt(last);
            return true;
        }

        return false;
    }
}
