using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Store;

[Serializable, NetSerializable]
public sealed class PurchasedItemRecord
{
    [DataField]
    public string? Name;

    [DataField]
    public Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> OriginalCost =
        new();

    [DataField]
    public Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> Cost =
        new();

    public PurchasedItemRecord() { }

    public PurchasedItemRecord(
        string? name,
        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> originalCost,
        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> cost
        )
    {
        Name = name;
        OriginalCost = originalCost;
        Cost = cost;
    }
}
