using System.Linq;
using Content.Shared._NC.Trade;
using Content.Shared.Stacks;


namespace Content.Server._NC.Trade;

public sealed partial class NcContractSystem : EntitySystem
{
    private readonly record struct ClaimContext(
        EntityUid Store,
        EntityUid User,
        EntityUid? Crate,
        NcStoreComponent Comp,
        ContractServerData Contract,
        List<ContractTargetServerData> Targets,
        Dictionary<(string ProtoId, PrototypeMatchMode MatchMode), int> RequiredByKey,
        List<EntityUid> UserItems,
        List<EntityUid>? CrateItems,
        List<ClaimTakeEntry> TakePlan
    );

    private bool TryPrepareClaimContext(
        EntityUid store,
        EntityUid user,
        string contractId,
        out ClaimContext ctx,
        out ClaimAttemptResult fail
    )
    {
        ctx = default;
        fail = ClaimAttemptResult.Fail(ClaimFailureReason.None);

        if (!TryComp(store, out NcStoreComponent? comp))
        {
            fail = ClaimAttemptResult.Fail(
                ClaimFailureReason.StoreMissing,
                $"Store {ToPrettyString(store)} has no NcStoreComponent.");
            return false;
        }

        if (!comp.Contracts.TryGetValue(contractId, out var contract))
        {
            fail = ClaimAttemptResult.Fail(
                ClaimFailureReason.ContractMissing,
                $"Store {ToPrettyString(store)} has no contract '{contractId}'.");
            return false;
        }

        var targets = GetEffectiveTargets(contract);
        if (targets.Count == 0)
        {
            fail = ClaimAttemptResult.Fail(
                ClaimFailureReason.NoValidTargets,
                $"Contract '{contractId}' has no valid targets.");
            return false;
        }

        var requiredByKey = new Dictionary<(string ProtoId, PrototypeMatchMode MatchMode), int>();
        foreach (var t in targets)
        {
            if (string.IsNullOrWhiteSpace(t.TargetItem) || t.Required <= 0)
            {
                fail = ClaimAttemptResult.Fail(
                    ClaimFailureReason.InvalidTarget,
                    $"Invalid target '{t.TargetItem}' (required={t.Required}).");
                return false;
            }

            var key = (t.TargetItem, t.MatchMode);
            if (!requiredByKey.TryAdd(key, t.Required))
                requiredByKey[key] = checked(requiredByKey[key] + t.Required);
        }
        _logic.ScanInventoryItems(user, _scratchUserItems);

        EntityUid? crateEntity = null;
        List<EntityUid>? crateItems = null;

        var crateUid = _logic.GetPulledClosedCrate(user);
        if (crateUid is { } c0 && Exists(c0))
        {
            crateEntity = c0;
            _logic.ScanInventoryItems(c0, _scratchCrateItems);
            crateItems = _scratchCrateItems;
        }

        var takePlan = new List<ClaimTakeEntry>(64);
        var virtualStackLeft = new Dictionary<EntityUid, int>();

        var orderedKeys = requiredByKey
            .OrderByDescending(k => GetProtoDepth(k.Key.ProtoId))
            .ThenBy(k => (int) k.Key.MatchMode)
            .ThenBy(k => k.Key.ProtoId, StringComparer.Ordinal)
            .ToArray();

        foreach (var kvp in orderedKeys)
        {
            var (protoId, matchMode) = kvp.Key;
            var required = kvp.Value;
            if (required <= 0)
                continue;

            var need = required;

            if (crateEntity is { } crate && crateItems != null)
            {
                var reserved = ReserveTakePlanFromItems(
                    crate,
                    crateItems,
                    protoId,
                    matchMode,
                    need,
                    virtualStackLeft,
                    takePlan);

                need -= reserved;
            }

            if (need > 0)
            {
                var reserved = ReserveTakePlanFromItems(
                    user,
                    _scratchUserItems,
                    protoId,
                    matchMode,
                    need,
                    virtualStackLeft,
                    takePlan);

                need -= reserved;
            }

            if (need > 0)
            {
                if (crateEntity == null)
                {
                    fail = ClaimAttemptResult.Fail(
                        ClaimFailureReason.MissingCrate,
                        $"need {required}x {protoId} (mode={matchMode}), missing {need}. Pull a closed crate to claim from it.");
                    return false;
                }

                fail = ClaimAttemptResult.Fail(
                    ClaimFailureReason.NotEnoughItems,
                    $"need {required}x {protoId} (mode={matchMode}), missing {need} after planning.");
                return false;
            }
        }

        ctx = new ClaimContext(
            store,
            user,
            crateEntity,
            comp,
            contract,
            targets,
            requiredByKey,
            _scratchUserItems,
            crateItems,
            takePlan);

        return true;
    }

    private int ReserveTakePlanFromItems(
        EntityUid root,
        List<EntityUid> items,
        string expectedProtoId,
        PrototypeMatchMode matchMode,
        int need,
        Dictionary<EntityUid, int> virtualStackLeft,
        List<ClaimTakeEntry> planOut
    )
    {
        if (need <= 0)
            return 0;

        var reserved = 0;

        if (TryGetStackTypeId(expectedProtoId, out var stackTypeId))
        {
            for (var i = 0; i < items.Count && reserved < need; i++)
            {
                var ent = items[i];
                if (ent == EntityUid.Invalid || !EntityManager.EntityExists(ent))
                    continue;

                if (_logic.IsProtectedFromDirectSale(root, ent))
                    continue;

                if (!TryComp(ent, out StackComponent? stack) || stack.StackTypeId != stackTypeId)
                    continue;

                var have = virtualStackLeft.TryGetValue(ent, out var v)
                    ? v
                    : Math.Max(stack.Count, 0);

                if (have <= 0)
                {
                    items[i] = EntityUid.Invalid;
                    continue;
                }

                var take = Math.Min(have, need - reserved);
                if (take <= 0)
                    continue;

                planOut.Add(new ClaimTakeEntry(root, ent, take, true));
                reserved += take;

                var left = have - take;
                if (left > 0)
                    virtualStackLeft[ent] = left;
                else
                {
                    virtualStackLeft.Remove(ent);
                    items[i] = EntityUid.Invalid;
                }
            }

            return reserved;
        }

        for (var i = 0; i < items.Count && reserved < need; i++)
        {
            var ent = items[i];
            if (ent == EntityUid.Invalid || !EntityManager.EntityExists(ent))
                continue;

            if (_logic.IsProtectedFromDirectSale(root, ent))
                continue;

            if (!TryComp(ent, out MetaDataComponent? meta) || meta.EntityPrototype == null)
                continue;

            var candidateId = meta.EntityPrototype.ID;

            var matches = matchMode == PrototypeMatchMode.Exact
                ? candidateId == expectedProtoId
                : candidateId == expectedProtoId || IsDescendantId(candidateId, expectedProtoId);

            if (!matches)
                continue;

            if (TryComp(ent, out StackComponent? st) && st.Count > 0)
            {
                var have = virtualStackLeft.TryGetValue(ent, out var v)
                    ? v
                    : Math.Max(st.Count, 0);

                if (have <= 0)
                {
                    items[i] = EntityUid.Invalid;
                    continue;
                }

                var take = Math.Min(have, need - reserved);
                if (take <= 0)
                    continue;

                planOut.Add(new ClaimTakeEntry(root, ent, take, true));
                reserved += take;

                var left = have - take;
                if (left > 0)
                    virtualStackLeft[ent] = left;
                else
                {
                    virtualStackLeft.Remove(ent);
                    items[i] = EntityUid.Invalid;
                }

                continue;
            }

            planOut.Add(new ClaimTakeEntry(root, ent, 1, false));
            reserved += 1;
            items[i] = EntityUid.Invalid;
        }

        return reserved;
    }

    private bool IsDescendantId(string candidateProtoId, string expectedAncestorId)
    {

        var ancestors = GetAncestorsInclusive(candidateProtoId);
        for (var i = 0; i < ancestors.Count; i++)
        {
            if (ancestors[i] == expectedAncestorId)
                return true;
        }

        return false;
    }
}
