using Content.Shared.FixedPoint;
using Content.Shared.Mind.Components;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Content.Server.StoreDiscount.Systems;

namespace Content.Server.Mind;

public sealed class PurchasesListSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreBuyFinishedEvent>(
            OnStoreBuyFinished,
            before: new[] { typeof(StoreDiscountSystem) }
        );
    }

    private void OnStoreBuyFinished(ref StoreBuyFinishedEvent ev)
    {
        if (!TryComp<MindContainerComponent>(ev.Buyer, out var mindContainer))
            return;

        if (mindContainer.Mind is null)
            return;

        var mind = mindContainer.Mind.Value;

        if (!TryComp<PurchasesListComponent>(mind, out var purchases))
            purchases = AddComp<PurchasesListComponent>(mind);

        var listing = ev.PurchasedItem;

        var originalCost = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(listing.OriginalCost);
        var cost = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(listing.Cost);

        var record = new PurchasedItemRecord(
            name: listing.Name,
            originalCost: originalCost,
            cost: cost
        );

        purchases.PurchaseHistory.Add(record);

        Dirty(mind, purchases);
    }
}
