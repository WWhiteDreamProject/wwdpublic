namespace Content.Shared.Store;


/// <summary>
/// Event of successfully finishing purchase in store (<see cref="StoreSystem"/>.
/// </summary>
/// <param name="StoreUid">EntityUid on which store is placed.</param>
/// <param name="PurchasedItem">ListingItem that was purchased.</param>
[ByRefEvent]
public readonly record struct StoreBuyFinishedEvent(
    EntityUid Buyer,
    EntityUid StoreUid,
    ListingDataWithCostModifiers PurchasedItem
);

// WD EDIT START - expose successful aggregate refunds to White purchase history.
/// <summary>
/// Raised after a store successfully refunds all purchases tracked in its current refund window.
/// </summary>
[ByRefEvent]
public readonly record struct StoreRefundFinishedEvent(
    EntityUid Buyer,
    EntityUid StoreUid
);
// WD EDIT END
