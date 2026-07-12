using System.Collections.Generic;
using System.Linq;
using Content.Shared._White.AntagPurchaseHistory;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

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
";

    [Test]
    public async Task PurchaseSnapshotsAntagCostsCurrenciesAndRefund()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entManager = server.EntMan;
        var testMap = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entManager.System<SharedMindSystem>();
            var roleSystem = entManager.System<SharedRoleSystem>();

            var antagBuyer = entManager.SpawnEntity("MobHuman", testMap.GridCoords);
            var antagMind = mindSystem.CreateMind(null);
            mindSystem.TransferTo(antagMind, antagBuyer, mind: antagMind.Comp);
            roleSystem.MindAddRole(antagMind, "MindRoleTraitor", mind: antagMind.Comp);
            Assert.That(roleSystem.MindIsAntagonist(antagMind));

            var storeUid = entManager.SpawnEntity("AntagPurchaseHistoryTestStore", testMap.GridCoords);
            var store = entManager.GetComponent<StoreComponent>(storeUid);
            var listing = store.FullListingsCatalog.Single(item => item.ID == "AntagPurchaseHistoryTestListing");
            listing.AddCostModifier("test-discount", new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>
            {
                ["Telecrystal"] = -3,
                ["WizCoin"] = -1,
            });

            var buyMessage = new StoreBuyListingMessage(listing.ID) { Actor = antagBuyer };
            entManager.EventBus.RaiseComponentEvent(storeUid, store, buyMessage);

            Assert.Multiple(() =>
            {
                Assert.That(entManager.HasComponent<AntagPurchaseHistoryComponent>(antagBuyer), Is.False,
                    "History must be stored on the mind, not its current body.");
                Assert.That(entManager.TryGetComponent<AntagPurchaseHistoryComponent>(antagMind, out var history));
                Assert.That(history!.Purchases, Has.Count.EqualTo(1));
            });

            var purchase = entManager.GetComponent<AntagPurchaseHistoryComponent>(antagMind).Purchases.Single();
            Assert.Multiple(() =>
            {
                Assert.That(purchase.ListingId.Id, Is.EqualTo("AntagPurchaseHistoryTestListing"));
                Assert.That(purchase.DisplayName, Is.Not.Null.And.Not.Empty);
                Assert.That(purchase.FinalCost["Telecrystal"], Is.EqualTo((FixedPoint2) 7));
                Assert.That(purchase.FinalCost["WizCoin"], Is.EqualTo((FixedPoint2) 3));
                Assert.That(purchase.OriginalCost["Telecrystal"], Is.EqualTo((FixedPoint2) 10));
                Assert.That(purchase.OriginalCost["WizCoin"], Is.EqualTo((FixedPoint2) 4));
                Assert.That(purchase.Refunded, Is.False);
            });

            // Mutating the live listing after purchase must not alter the stored snapshot.
            listing.RemoveCostModifier("test-discount");
            Assert.That(purchase.FinalCost["Telecrystal"], Is.EqualTo((FixedPoint2) 7));

            var refundMessage = new StoreRequestRefundMessage { Actor = antagBuyer };
            entManager.EventBus.RaiseComponentEvent(storeUid, store, refundMessage);
            Assert.That(purchase.Refunded, Is.True);

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
