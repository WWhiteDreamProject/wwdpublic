using Content.Shared._NC.Trade;

namespace Content.Server._NC.Trade;

public sealed partial class NcStoreLogicSystem
{

    public NcInventorySnapshot BuildInventorySnapshot(EntityUid root) => _inventory.BuildInventorySnapshot(root);

    public void FillInventorySnapshot(EntityUid root, NcInventorySnapshot buffer) => _inventory.FillInventorySnapshot(root, buffer);

    public void ScanInventory(EntityUid root, List<EntityUid> itemsBuffer, NcInventorySnapshot snapshotBuffer) => _inventory.ScanInventory(root, itemsBuffer, snapshotBuffer);

    public void ScanInventoryItems(EntityUid root, List<EntityUid> itemsBuffer) => _inventory.ScanInventoryItems(root, itemsBuffer);

    public int GetOwnedFromSnapshot(in NcInventorySnapshot snapshot, string productProtoId, PrototypeMatchMode matchMode) => _inventory.GetOwnedFromSnapshot(snapshot, productProtoId, matchMode);

    public bool TryTakeProductUnitsFromRootCached(EntityUid root, string protoId, int amount, PrototypeMatchMode matchMode) => _inventory.TryTakeProductUnitsFromRootCached(root, protoId, amount, matchMode);

    public bool TryTakeProductUnitsFromCachedList(
        EntityUid root,
        List<EntityUid> cachedItems,
        string protoId,
        int amount,
        PrototypeMatchMode matchMode) => _inventory.TryTakeProductUnitsFromCachedList(root, cachedItems, protoId, amount, matchMode);

    public bool IsProtectedFromDirectSale(EntityUid root, EntityUid item) => _inventory.IsProtectedFromDirectSale(root, item);
}
