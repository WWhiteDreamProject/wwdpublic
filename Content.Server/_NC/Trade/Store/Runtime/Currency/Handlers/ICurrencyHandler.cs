namespace Content.Server._NC.Trade;

/// <summary>
/// Currency handler abstraction.
///
/// A currency id is an opaque string used by listings and presets.
/// Implementations (stack items, virtual balances, bank accounts, etc.) are resolved via <see cref="CanHandle"/>.
/// </summary>
public interface ICurrencyHandler
{
    /// <summary>
    /// Returns true if this handler can operate on the provided currency id.
    /// </summary>
    bool CanHandle(string currencyId);

    /// <summary>
    /// Attempts to extract a balance for this currency using an inventory snapshot.
    /// For non-inventory currencies (virtual/bank), implementations may ignore the snapshot and query components.
    /// </summary>
    bool TryGetBalance(in NcInventorySnapshot snapshot, string currencyId, out int balance);

    /// <summary>
    /// Attempts to take a positive amount of currency from the user.
    /// </summary>
    bool TryTake(EntityUid user, string currencyId, int amount);

    /// <summary>
    /// Gives a positive amount of currency to the user.
    /// </summary>
    bool TryGiveCurrency(EntityUid user, string currencyId, int amount);
}
