using System.Linq;
using Content.Server.StoreDiscount.Systems;
using Content.Shared._White.AntagPurchaseHistory;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Server._White.AntagPurchaseHistory;

/// <summary>
/// Records successful store purchases made by antagonist minds.
/// </summary>
public sealed class AntagPurchaseHistorySystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();

        // StoreDiscountSystem removes exhausted modifiers from PurchasedItem. Snapshot Cost before that mutation.
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnBuyFinished, before: new[] { typeof(StoreDiscountSystem) });
        SubscribeLocalEvent<StoreRefundFinishedEvent>(OnRefundFinished);
    }

    private void OnBuyFinished(ref StoreBuyFinishedEvent args)
    {
        if (!_mind.TryGetMind(args.Buyer, out var mindUid, out _) ||
            !_roles.MindIsAntagonist(mindUid))
        {
            return;
        }

        var listing = args.PurchasedItem;
        var displayName = ListingLocalisationHelpers.GetLocalisedNameOrEntityName(listing, _prototypes);
        var history = EnsureComp<AntagPurchaseHistoryComponent>(mindUid);

        history.Purchases.Add(new AntagPurchaseRecord(
            listing.ID,
            string.IsNullOrWhiteSpace(displayName) ? null : displayName,
            listing.Cost.ToDictionary(pair => pair.Key, pair => pair.Value),
            listing.OriginalCost.ToDictionary(pair => pair.Key, pair => pair.Value),
            args.StoreUid));
    }

    private void OnRefundFinished(ref StoreRefundFinishedEvent args)
    {
        if (!_mind.TryGetMind(args.Buyer, out var mindUid, out _) ||
            !TryComp<AntagPurchaseHistoryComponent>(mindUid, out var history))
        {
            return;
        }

        // StoreSystem refunds BoughtEntities and BalanceSpent in aggregate, without a per-listing mapping.
        // Therefore every still-active purchase from this store is marked refunded together.
        foreach (var purchase in history.Purchases)
        {
            if (purchase.StoreUid == args.StoreUid)
                purchase.Refunded = true;
        }
    }
}
