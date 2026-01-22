using Robust.Shared.Serialization;

namespace Content.Shared._NC.Trade;

[Serializable, NetSerializable]
public sealed class StoreBuyListingBoundUiMessage : BoundUserInterfaceMessage
{
    public StoreBuyListingBoundUiMessage(string id, int count)
    {
        Id = id;
        Count = count;
    }

    public string Id { get; }
    public int Count { get; }
}

[Serializable, NetSerializable]
public sealed class StoreSellListingBoundUiMessage : BoundUserInterfaceMessage
{
    public StoreSellListingBoundUiMessage(string id, int count, bool fromCrate = false)
    {
        Id = id;
        Count = count;
        FromCrate = fromCrate;
    }

    public string Id { get; }
    public int Count { get; }
    public bool FromCrate { get; }
}


[Serializable, NetSerializable]
public sealed class StoreMassSellPulledCrateBoundUiMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class ClaimContractBoundMessage : BoundUserInterfaceMessage
{
    public ClaimContractBoundMessage(string contractId)
    {
        ContractId = contractId;
    }

    public string ContractId { get; }
}

[Serializable, NetSerializable]
public sealed class RequestUiRefreshMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class RequestContractsRefreshBoundMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class StoreSetVisibleListingsBoundUiMessage : BoundUserInterfaceMessage
{
    public StoreSetVisibleListingsBoundUiMessage(string[] ids)
    {
        Ids = ids;
    }

    public string[] Ids { get; }
}
