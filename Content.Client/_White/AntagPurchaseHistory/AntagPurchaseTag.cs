using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Client.Actions;
using Content.Shared._White.AntagPurchaseHistory;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._White.AntagPurchaseHistory;

/// <summary>
/// Renders a store listing icon embedded in rich text and supplies its historical purchase price as a tooltip.
/// </summary>
public sealed class AntagPurchaseTag : IMarkupTagHandler
{
    private static readonly SpriteSpecifier ErrorIcon =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/error.rsi"), "error");

    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public string Name => AntagPurchaseMarkup.TagName;

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (node.Closing ||
            !node.Value.TryGetString(out var listingId) ||
            !TryGetCost(node, AntagPurchaseMarkup.FinalCostAttribute, out var finalCost) ||
            !TryGetCost(node, AntagPurchaseMarkup.OriginalCostAttribute, out var originalCost))
        {
            return false;
        }

        var texture = GetListingTexture(listingId);
        var listingName = GetListingName(listingId);
        var icon = new TextureRect
        {
            Texture = texture,
            SetSize = new Vector2(AntagPurchaseMarkup.IconSize, AntagPurchaseMarkup.IconSize),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            MouseFilter = Control.MouseFilterMode.Stop,
            TooltipSupplier = _ => CreatePriceTooltip(listingName, originalCost, finalCost),
        };

        control = icon;
        return true;
    }

    private bool TryGetCost(
        MarkupNode node,
        string attribute,
        out Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> cost)
    {
        cost = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>();
        return node.Attributes.TryGetValue(attribute, out var parameter) &&
               parameter.TryGetString(out var serialized) &&
               AntagPurchaseMarkup.TryDeserializeCost(serialized, out cost);
    }

    private Texture? GetListingTexture(string listingId)
    {
        // The stripped-down integration-test client does not load rendering systems.
        // In the game SpriteSystem is always available; keeping the control lets markup and tooltip tests
        // run headlessly.
        if (!_entities.EntitySysManager.TryGetEntitySystem<SpriteSystem>(out var sprites))
            return null;

        if (!_prototypes.TryIndex<ListingPrototype>(listingId, out var listing))
            return sprites.Frame0(ErrorIcon);

        if (listing.Icon != null)
            return sprites.Frame0(listing.Icon);

        if (listing.ProductEntity != null)
            return sprites.GetPrototypeIcon(listing.ProductEntity.Value).Default;

        if (listing.ProductAction != null)
        {
            var actionUid = _entities.Spawn(listing.ProductAction);
            try
            {
                if (_entities.System<ActionsSystem>().TryGetActionData(actionUid, out var action) &&
                    action.Icon != null)
                {
                    return sprites.Frame0(action.Icon);
                }
            }
            finally
            {
                _entities.DeleteEntity(actionUid);
            }
        }

        return sprites.Frame0(ErrorIcon);
    }

    private string GetListingName(string listingId)
    {
        if (!_prototypes.TryIndex<ListingPrototype>(listingId, out var listing))
            return listingId;

        var name = ListingLocalisationHelpers.GetLocalisedNameOrEntityName(listing, _prototypes);
        return string.IsNullOrWhiteSpace(name) ? listingId : name;
    }

    private Tooltip CreatePriceTooltip(
        string listingName,
        IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2> originalCost,
        IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2> finalCost)
    {
        var tooltip = new Tooltip();
        var message = new FormattedMessage();
        message.AddText($"{listingName}: ");

        if (AntagPurchaseMarkup.HasDiscount(originalCost, finalCost))
        {
            message.PushColor(Color.Red);
            message.AddText(GetPriceString(originalCost));
            message.Pop();
            message.AddText(" | ");
        }

        message.AddText(GetPriceString(finalCost));
        tooltip.SetMessage(message);
        return tooltip;
    }

    private string GetPriceString(IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2> cost)
    {
        if (cost.Count == 0)
            return Loc.GetString("store-currency-free");

        return string.Join(", ", cost.Select(pair =>
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
