using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Server._White.AntagPurchaseHistory;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Objectives;
using Content.Shared._White.AntagPurchaseHistory;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._White;

[TestFixture]
public sealed class AntagPurchaseHistoryTests
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: AntagPurchaseHistoryTestStore
  components:
  - type: Store
    categories:
    - UplinkWeaponry
    currencyWhitelist:
    - Telecrystal
    - WizCoin
    balance:
      Telecrystal: 100
      WizCoin: 100
    refundAllowed: true

- type: entity
  id: AntagPurchaseHistoryTestProduct
  parent: BaseItem

- type: listing
  id: AntagPurchaseHistoryTestListing
  name: antag purchase history test item
  productEntity: AntagPurchaseHistoryTestProduct
  cost:
    Telecrystal: 10
    WizCoin: 4
  categories:
  - UplinkWeaponry

- type: entity
  id: AntagPurchaseHistoryTestRule
  parent: BaseGameRule
  components:
  - type: GenericAntagRule
    agentName: traitor-round-end-agent-name
    objectives: []
";

    [Test]
    public async Task PurchaseSnapshotsAntagCostsCurrenciesAndRefund()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var client = pair.Client;
        var entManager = server.EntMan;
        var testMap = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        EntityUid antagBuyer = default;
        Entity<MindComponent> antagMind = default;
        EntityUid storeUid = default;
        string roundEndMarkup = string.Empty;

        await server.WaitAssertion(() =>
        {
            var mindSystem = entManager.System<SharedMindSystem>();
            var roleSystem = entManager.System<SharedRoleSystem>();

            antagBuyer = entManager.SpawnEntity("MobHuman", testMap.GridCoords);
            antagMind = mindSystem.CreateMind(null);
            mindSystem.TransferTo(antagMind, antagBuyer, mind: antagMind.Comp);
            roleSystem.MindAddRole(antagMind, "MindRoleTraitor", mind: antagMind.Comp);
            Assert.That(roleSystem.MindIsAntagonist(antagMind));
            Assert.That(entManager.System<GenericAntagRuleSystem>().StartRule(
                "AntagPurchaseHistoryTestRule",
                antagMind,
                out _,
                out _));

            storeUid = entManager.SpawnEntity("AntagPurchaseHistoryTestStore", testMap.GridCoords);
            var store = entManager.GetComponent<StoreComponent>(storeUid);
            var listing = store.FullListingsCatalog.Single(item => item.ID == "AntagPurchaseHistoryTestListing");
            listing.AddCostModifier("test-discount", new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>
            {
                ["Telecrystal"] = -3,
                ["WizCoin"] = -1,
            });

            var buyMessage = new StoreBuyListingMessage(listing.ID) { Actor = antagBuyer };
            entManager.EventBus.RaiseComponentEvent(storeUid, store, buyMessage);
            entManager.EventBus.RaiseComponentEvent(storeUid, store, buyMessage);

            Assert.Multiple(() =>
            {
                Assert.That(entManager.HasComponent<AntagPurchaseHistoryComponent>(antagBuyer), Is.False,
                    "History must be stored on the mind, not its current body.");
                Assert.That(entManager.TryGetComponent<AntagPurchaseHistoryComponent>(antagMind, out var history));
                Assert.That(history!.Purchases, Has.Count.EqualTo(2));
            });

            var purchases = entManager.GetComponent<AntagPurchaseHistoryComponent>(antagMind).Purchases;
            var purchase = purchases[0];
            Assert.Multiple(() =>
            {
                Assert.That(purchase.ListingId.Id, Is.EqualTo("AntagPurchaseHistoryTestListing"));
                Assert.That(purchase.FinalCost["Telecrystal"], Is.EqualTo((FixedPoint2) 7));
                Assert.That(purchase.FinalCost["WizCoin"], Is.EqualTo((FixedPoint2) 3));
                Assert.That(purchase.OriginalCost["Telecrystal"], Is.EqualTo((FixedPoint2) 10));
                Assert.That(purchase.OriginalCost["WizCoin"], Is.EqualTo((FixedPoint2) 4));
                Assert.That(purchase.Refunded, Is.False);
            });

            // Mutating the live listing after purchase must not alter the stored snapshot.
            listing.RemoveCostModifier("test-discount");
            Assert.That(purchase.FinalCost["Telecrystal"], Is.EqualTo((FixedPoint2) 7));

            entManager.EventBus.RaiseComponentEvent(storeUid, store, buyMessage);
            Assert.That(purchases, Has.Count.EqualTo(3));
            Assert.Multiple(() =>
            {
                Assert.That(purchases[2].FinalCost["Telecrystal"], Is.EqualTo((FixedPoint2) 10));
                Assert.That(purchases[2].FinalCost["WizCoin"], Is.EqualTo((FixedPoint2) 4));
            });

            roundEndMarkup = entManager.System<AntagPurchaseHistorySystem>().GetRoundEndMarkup(antagMind);
            var additionalInfo = new ObjectivesTextGetAdditionalInfoEvent(new List<string>());
            entManager.EventBus.RaiseLocalEvent(antagMind, ref additionalInfo);
            var roundEnd = new RoundEndTextAppendEvent();
            entManager.EventBus.RaiseEvent(EventSource.Local, roundEnd);
            var parsed = FormattedMessage.FromMarkupOrThrow(roundEndMarkup);
            var iconNodes = parsed.Nodes
                .Where(node => node is { Closing: false, Name: AntagPurchaseMarkup.TagName })
                .ToArray();
            Assert.Multiple(() =>
            {
                Assert.That(parsed.ToString(), Does.StartWith("("));
                Assert.That(parsed.ToString(), Does.Contain("24"));
                Assert.That(parsed.ToString(), Does.Contain(", 10"));
                Assert.That(parsed.ToString(), Does.EndWith("[2x ], []"));
                Assert.That(additionalInfo.Lines, Is.EqualTo(new[] { roundEndMarkup }));
                Assert.That(antagMind.Comp.Objectives, Is.Empty);
                Assert.That(roundEnd.Text, Does.Contain(roundEndMarkup));
                Assert.That(parsed.Nodes.Any(node => node.Name == "font"), Is.False);
                Assert.That(iconNodes, Has.Length.EqualTo(2));
                Assert.That(iconNodes[0].Value.StringValue, Is.EqualTo("AntagPurchaseHistoryTestListing"));
                Assert.That(iconNodes[0].Attributes[AntagPurchaseMarkup.FinalCostAttribute].StringValue,
                    Is.EqualTo("Telecrystal:700;WizCoin:300"));
                Assert.That(iconNodes[1].Attributes[AntagPurchaseMarkup.FinalCostAttribute].StringValue,
                    Is.EqualTo("Telecrystal:1000;WizCoin:400"));
            });
        });

        await client.WaitPost(() =>
        {
            var label = new RichTextLabel();
            label.SetMessage(FormattedMessage.FromMarkupOrThrow(roundEndMarkup));
            var icons = label.Controls.Cast<TextureRect>().ToArray();

            Assert.That(icons, Has.Length.EqualTo(2));
            Assert.That(
                icons.Select(icon => icon.SetSize),
                Is.All.EqualTo(new Vector2(AntagPurchaseMarkup.IconSize, AntagPurchaseMarkup.IconSize)));
            Assert.That(icons, Has.All.Property(nameof(TextureRect.TooltipSupplier)).Not.Null);

            var discountedTooltip = icons[0].TooltipSupplier!(icons[0]) as Tooltip;
            var fullPriceTooltip = icons[1].TooltipSupplier!(icons[1]) as Tooltip;
            Assert.Multiple(() =>
            {
                Assert.That(discountedTooltip, Is.Not.Null);
                Assert.That(discountedTooltip!.Text,
                    Does.StartWith("antag purchase history test item: "));
                Assert.That(discountedTooltip.Text, Does.Contain("[color=red]"));
                Assert.That(discountedTooltip.Text, Does.Contain(" | "));
                Assert.That(fullPriceTooltip, Is.Not.Null);
                Assert.That(fullPriceTooltip!.Text,
                    Does.StartWith("antag purchase history test item: "));
                Assert.That(fullPriceTooltip.Text, Does.Not.Contain("[color=red]"));
                Assert.That(fullPriceTooltip.Text, Does.Not.Contain(" | "));
            });
        });

        await server.WaitAssertion(() =>
        {
            var mindSystem = entManager.System<SharedMindSystem>();
            var roleSystem = entManager.System<SharedRoleSystem>();
            var store = entManager.GetComponent<StoreComponent>(storeUid);

            var refundMessage = new StoreRequestRefundMessage { Actor = antagBuyer };
            entManager.EventBus.RaiseComponentEvent(storeUid, store, refundMessage);
            var purchases = entManager.GetComponent<AntagPurchaseHistoryComponent>(antagMind).Purchases;
            Assert.Multiple(() =>
            {
                Assert.That(purchases, Has.All.Property(nameof(AntagPurchaseRecord.Refunded)).True);
                Assert.That(entManager.System<AntagPurchaseHistorySystem>().GetRoundEndMarkup(antagMind), Is.Empty);
            });

            var nonAntagBuyer = entManager.SpawnEntity("MobHuman", testMap.GridCoords);
            var nonAntagMind = mindSystem.CreateMind(null);
            mindSystem.TransferTo(nonAntagMind, nonAntagBuyer, mind: nonAntagMind.Comp);
            Assert.That(roleSystem.MindIsAntagonist(nonAntagMind), Is.False);

            var secondStoreUid = entManager.SpawnEntity("AntagPurchaseHistoryTestStore", testMap.GridCoords);
            var secondStore = entManager.GetComponent<StoreComponent>(secondStoreUid);
            var nonAntagBuy = new StoreBuyListingMessage("AntagPurchaseHistoryTestListing") { Actor = nonAntagBuyer };
            entManager.EventBus.RaiseComponentEvent(secondStoreUid, secondStore, nonAntagBuy);

            Assert.That(entManager.HasComponent<AntagPurchaseHistoryComponent>(nonAntagMind), Is.False);
        });

        await pair.CleanReturnAsync();
    }
}
