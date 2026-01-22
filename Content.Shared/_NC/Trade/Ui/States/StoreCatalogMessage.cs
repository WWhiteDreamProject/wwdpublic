using Robust.Shared.Serialization;

namespace Content.Shared._NC.Trade;

[Serializable, NetSerializable]
public sealed class StoreCatalogMessage : BoundUserInterfaceMessage
{
    public StoreCatalogMessage(
        int catalogRevision,
        List<StoreListingStaticData> listings,
        bool hasBuyTab,
        bool hasSellTab,
        bool hasContractsTab)
    {
        CatalogRevision = catalogRevision;
        Listings = listings;
        HasBuyTab = hasBuyTab;
        HasSellTab = hasSellTab;
        HasContractsTab = hasContractsTab;
    }

    public int CatalogRevision { get; }
    public List<StoreListingStaticData> Listings { get; }
    public bool HasBuyTab { get; }
    public bool HasSellTab { get; }
    public bool HasContractsTab { get; }
}

[Serializable, NetSerializable]
public sealed class StoreListingStaticData
{
    public StoreListingStaticData(
        string id,
        StoreMode mode,
        string category,
        string productEntity,
        int basePrice,
        string currencyId,
        int unitsPerPurchase)
    {
        Id = id;
        Mode = mode;
        Category = category;
        ProductEntity = productEntity;
        BasePrice = basePrice;
        CurrencyId = currencyId;
        UnitsPerPurchase = unitsPerPurchase;
    }

    public string Id { get; }
    public StoreMode Mode { get; }
    public string Category { get; }
    public string ProductEntity { get; }
    public int BasePrice { get; }
    public string CurrencyId { get; }
    public int UnitsPerPurchase { get; }
}
