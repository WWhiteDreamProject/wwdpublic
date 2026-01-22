using System;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Trade;

/// <summary>
/// Flavor of a listing entry on the client (base listing vs derived virtual entries).
/// </summary>
[Serializable, NetSerializable]
public enum StoreListingFlavor : byte
{
    Base = 0,
    Ready = 1,
    Crate = 2
}


/// <summary>
/// Client-side listing view model (static + dynamic fields).
/// Constructed on the client from <see cref="StoreListingStaticData"/> and <see cref="StoreDynamicState"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class StoreListingData
{
    public string Category = string.Empty;
    public string CurrencyId = string.Empty;
    public string Id = string.Empty;

    /// <summary>Base listing id used for server transactions.</summary>
    public string ListingId = string.Empty;

    /// <summary>Client-only flavor to distinguish derived entries.</summary>
    public StoreListingFlavor Flavor = StoreListingFlavor.Base;
    public StoreMode Mode;

    public static string MakeUiId(string listingId, StoreListingFlavor flavor)
    {
        // Unit Separator (0x1F) is extremely unlikely to appear in prototype ids; we use it to avoid collisions.
        return listingId + "\u001f" + ((byte) flavor).ToString();
    }

    // Dynamic
    public int Owned;
    public int Price;
    public int UnitsPerPurchase = 1;
    public string ProductEntity = string.Empty;
    public int Remaining = -1;

    public StoreListingData() { }

    public StoreListingData(
        string listingId,
        StoreListingFlavor flavor,
        string productEntity,
        int price,
        string category,
        string currencyId,
        StoreMode mode,
        int owned = 0,
        int remaining = -1,
        int unitsPerPurchase = 1)
    {
        ListingId = listingId;
        Flavor = flavor;
        Id = flavor == StoreListingFlavor.Base ? listingId : MakeUiId(listingId, flavor);
        ProductEntity = productEntity;
        Price = price;
        Category = category;
        CurrencyId = currencyId;
        Mode = mode;
        Owned = owned;
        Remaining = remaining;
        UnitsPerPurchase = Math.Max(1, unitsPerPurchase);
    }

    public StoreListingData(
        string id,
        string productEntity,
        int price,
        string category,
        string currencyId,
        StoreMode mode,
        int owned = 0,
        int remaining = -1,
        int unitsPerPurchase = 1)
    {
        Id = id;
        ListingId = id;
        Flavor = StoreListingFlavor.Base;
        ProductEntity = productEntity;
        Price = price;
        Category = category;
        CurrencyId = currencyId;
        Mode = mode;
        Owned = owned;
        Remaining = remaining;
        UnitsPerPurchase = Math.Max(1, unitsPerPurchase);
    }
}
