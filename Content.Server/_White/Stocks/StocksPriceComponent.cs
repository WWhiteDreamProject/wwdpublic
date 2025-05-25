namespace Content.Server._White.stocks;

/// <summary>
/// Multiplies the cost by a price of a stock
/// </summary>
[RegisterComponent]
public sealed partial class StocksPriceComponent : Component
{
    /// <summary>
    /// LocId of the stock to use.
    /// </summary>
    [DataField]
    public LocId Stock;

    /// <summary>
    /// How much to multiply the cost of the stock for the final price.
    /// </summary>
    [DataField]
    public float Multiplier = 1f;
}
