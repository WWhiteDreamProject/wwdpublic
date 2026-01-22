using Content.Shared._NC.Trade;


namespace Content.Client._NC.Trade;


public sealed partial class NcStoreMenu
{
    private void RefreshListingsDynamicOnly()
    {
        if (_disposed)
            return;

        BuyView.UpdateDynamicOnly(GetBalanceForCurrency);
        SellView.UpdateDynamicOnly(static _ => int.MaxValue);
    }
}
