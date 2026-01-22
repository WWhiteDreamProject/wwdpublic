using Robust.Shared.Serialization;

namespace Content.Shared._NC.Trade;

[Serializable, NetSerializable]
public sealed class StoreDynamicState : BoundUserInterfaceState
{
    public StoreDynamicState(
        int revision,
        int catalogRevision,
        Dictionary<string, int> balanceByCurrency,
        Dictionary<string, int> remainingById,
        Dictionary<string, int> ownedById,
        Dictionary<string, int> crateUnitsById,
        Dictionary<string, int> massSellTotals,
        List<ContractClientData> contracts,
        bool hasBuyTab,
        bool hasSellTab,
        bool hasContractsTab)
    {
        Revision = revision;
        CatalogRevision = catalogRevision;
        BalanceByCurrency = balanceByCurrency;
        RemainingById = remainingById;
        OwnedById = ownedById;
        CrateUnitsById = crateUnitsById;
        MassSellTotals = massSellTotals;
        Contracts = contracts;
        HasBuyTab = hasBuyTab;
        HasSellTab = hasSellTab;
        HasContractsTab = hasContractsTab;
    }

    public int Revision { get; }
    public int CatalogRevision { get; }

    public Dictionary<string, int> BalanceByCurrency { get; }
    public Dictionary<string, int> RemainingById { get; }
    public Dictionary<string, int> OwnedById { get; }
    public Dictionary<string, int> CrateUnitsById { get; }

    public Dictionary<string, int> MassSellTotals { get; }

    public List<ContractClientData> Contracts { get; }

    public bool HasBuyTab { get; }
    public bool HasSellTab { get; }
    public bool HasContractsTab { get; }
}
