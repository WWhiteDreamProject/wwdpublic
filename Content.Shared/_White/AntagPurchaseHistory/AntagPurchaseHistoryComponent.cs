using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.AntagPurchaseHistory;

/// <summary>
/// Stores an antagonist's store purchase history on their mind entity.
/// </summary>
[RegisterComponent]
public sealed partial class AntagPurchaseHistoryComponent : Component
{
    [ViewVariables]
    public List<AntagPurchaseRecord> Purchases = new();
}

/// <summary>
/// A snapshot of a store listing at the time it was purchased.
/// </summary>
public sealed class AntagPurchaseRecord
{
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<ListingPrototype> ListingId { get; }

    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> FinalCost { get; }

    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> OriginalCost { get; }

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid StoreUid { get; }

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Refunded { get; set; }

    public AntagPurchaseRecord(
        ProtoId<ListingPrototype> listingId,
        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> finalCost,
        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> originalCost,
        EntityUid storeUid)
    {
        ListingId = listingId;
        FinalCost = finalCost;
        OriginalCost = originalCost;
        StoreUid = storeUid;
    }
}
