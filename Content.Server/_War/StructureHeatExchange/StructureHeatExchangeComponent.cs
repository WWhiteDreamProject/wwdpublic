using Content.Server.Temperature.Components;

namespace Content.Server._War.StructureHeatExchange;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class StructureHeatExchangeComponent : Component
{
    [DataField]
    public List<Vector2i>? CachedAdjacentTiles;

    [DataField]
    public List<StructureHeatExchangerCacheEntry>? CachedAdjacentHeatExchangers;

    [DataField]
    public EntityUid? Parent;

    [DataField]
    public TemperatureComponent? TempComp;
}
