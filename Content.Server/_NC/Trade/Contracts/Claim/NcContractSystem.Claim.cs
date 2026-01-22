namespace Content.Server._NC.Trade;

public sealed partial class NcContractSystem : EntitySystem
{
    public bool TryClaim(EntityUid store, EntityUid user, string contractId)
    {
        var res = TryClaimDetailed(store, user, contractId);
        if (!res.Success)
        {
            if (res.Reason is ClaimFailureReason.NotEnoughItems or ClaimFailureReason.NoValidTargets or ClaimFailureReason.MissingCrate)
                Sawmill.Info($"[Claim] Failed ({res.Reason}) '{contractId}' on {ToPrettyString(store)}: {res.Details}");
            else
                Sawmill.Warning($"[Claim] Failed ({res.Reason}) '{contractId}' on {ToPrettyString(store)}: {res.Details}");
        }

        return res.Success;
    }

    private ClaimAttemptResult TryClaimDetailed(EntityUid store, EntityUid user, string contractId)
    {
        if (!TryPrepareClaimContext(store, user, contractId, out var ctx, out var prepFail))
            return prepFail;

        if (!TryExecuteClaimTakePlan(ctx, out var execFail))
            return execFail;

        FinalizeClaim(ctx, contractId);
        return ClaimAttemptResult.Ok();
    }

}
