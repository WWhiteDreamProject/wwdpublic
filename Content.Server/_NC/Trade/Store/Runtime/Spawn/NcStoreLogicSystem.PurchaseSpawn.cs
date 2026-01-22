using Robust.Shared.Prototypes;

namespace Content.Server._NC.Trade;

public sealed partial class NcStoreLogicSystem
{

    private int SpawnPurchasedProduct(
        EntityUid user,
        string productEntity,
        EntityPrototype productProto,
        int amount,
        int unitPrice,
        string currency
    ) =>
        _spawnService.SpawnPurchasedProduct(user, productEntity, productProto, amount, unitPrice, currency);
}
