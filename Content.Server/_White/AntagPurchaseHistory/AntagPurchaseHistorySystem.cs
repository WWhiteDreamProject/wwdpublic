using System.Linq;
using Content.Server.Objectives;
using Content.Server.StoreDiscount.Systems;
using Content.Shared._White.AntagPurchaseHistory;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

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
        SubscribeLocalEvent<AntagPurchaseHistoryComponent, ObjectivesTextGetAdditionalInfoEvent>(OnGetAdditionalInfo);
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

    private void OnGetAdditionalInfo(
        EntityUid uid,
        AntagPurchaseHistoryComponent component,
        ref ObjectivesTextGetAdditionalInfoEvent args)
    {
        var markup = GetRoundEndMarkup(uid, component);
        if (!string.IsNullOrEmpty(markup))
            args.Lines.Add(markup);
    }

    /// <summary>
    /// Builds the inline markup for all purchases that were not refunded.
    /// Repeated purchases of the same listing at the same final price are grouped together.
    /// </summary>
    public string GetRoundEndMarkup(EntityUid mindUid, AntagPurchaseHistoryComponent? history = null)
    {
        if (!Resolve(mindUid, ref history, false))
            return string.Empty;

        var purchases = history.Purchases
            .Where(purchase => !purchase.Refunded)
            .ToList();

        if (purchases.Count == 0)
            return string.Empty;

        var groupedPurchases = purchases
            .GroupBy(purchase => (
                purchase.ListingId,
                FinalCost: AntagPurchaseMarkup.SerializeCost(purchase.FinalCost)))
            .ToList();

        var totalCost = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>();
        foreach (var purchase in purchases)
        {
            foreach (var (currency, amount) in purchase.FinalCost)
                totalCost[currency] = totalCost.GetValueOrDefault(currency) + amount;
        }

        var message = new FormattedMessage();
        message.AddText(Loc.GetString(
            "antag-purchase-history-used",
            ("amounts", GetPriceString(totalCost))));
        message.AddText(" ");

        for (var i = 0; i < groupedPurchases.Count; i++)
        {
            var group = groupedPurchases[i];
            var purchase = group.First();

            if (i > 0)
                message.AddText(", ");

            // The opening bracket must be escaped because this FormattedMessage is converted back to markup
            // before it is parsed by the client.
            message.AddText(FormattedMessage.EscapeText("["));
            if (group.Count() > 1)
                message.AddText($"{group.Count()}x ");

            var attributes = new Dictionary<string, MarkupParameter>
            {
                [AntagPurchaseMarkup.FinalCostAttribute] = new(group.Key.FinalCost),
                [AntagPurchaseMarkup.OriginalCostAttribute] = new(
                    AntagPurchaseMarkup.SerializeCost(purchase.OriginalCost)),
            };
            message.PushTag(
                new MarkupNode(
                    AntagPurchaseMarkup.TagName,
                    new MarkupParameter(purchase.ListingId.Id),
                    attributes),
                selfClosing: true);
            message.AddText("]");
        }

        return message.ToMarkup();
    }

    private string GetPriceString(IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2> cost)
    {
        if (cost.Count == 0)
            return Loc.GetString("store-currency-free");

        return string.Join(", ", cost
            .OrderBy(pair => pair.Key.Id)
            .Select(pair =>
            {
                if (!_prototypes.TryIndex(pair.Key, out CurrencyPrototype? currency))
                    return $"{pair.Value} {pair.Key.Id}";

                return Loc.GetString(
                    "store-ui-price-display",
                    ("amount", pair.Value),
                    ("currency", Loc.GetString(currency.DisplayName, ("amount", pair.Value))));
            }));
    }
}
