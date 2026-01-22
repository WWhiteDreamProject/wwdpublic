using Robust.Shared.Serialization;

namespace Content.Shared._NC.Trade;

[Serializable, NetSerializable]
public enum StoreMode
{
    Buy,
    Sell,
    Exchange
}
