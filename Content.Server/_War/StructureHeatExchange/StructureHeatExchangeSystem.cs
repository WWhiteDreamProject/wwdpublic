using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using YamlDotNet.Core.Tokens;

namespace Content.Server._War.StructureHeatExchange;

/// <summary>
/// this is meant to be used only for things that dont move.
/// TODO: cvar shit
/// </summary>
public sealed class StructureHeatExchangeSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private Dictionary<EntityUid, HashSet<StructureHeatExchangerCacheEntry>> _cachedHeatExchangers = new();
    public float Multiplier = 5; // 10 TOO MUCH BRO!!!
    public bool Enabled = true;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StructureHeatExchangeComponent, AtmosDeviceUpdateEvent>(ProcessDevice);
        SubscribeLocalEvent<StructureHeatExchangeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StructureHeatExchangeComponent, ComponentShutdown>(OnCompShutdown);

    }

    private void OnMapInit(EntityUid uid, StructureHeatExchangeComponent comp, MapInitEvent ev)
    {
        if (!TryComp<TemperatureComponent>(uid, out var tempComp))
        {
            RemComp<StructureHeatExchangeComponent>(uid);
            return;
        }

        if (!_transform.TryGetGridTilePosition(uid, out var tile))
            return;

        var parent = Transform(uid).ParentUid;
        comp.Parent = parent;

        if (!TryComp<GridAtmosphereComponent>(parent, out var grid))
        {
            RemComp<StructureHeatExchangeComponent>(uid);
            return;
        }

        comp.TempComp = tempComp;

        // cache all the adjacent tiles
        var adjacent = new List<Vector2i>
        {
            tile + new Vector2i(1, 0),
            tile + new Vector2i(0, 1),
            tile + new Vector2i(-1, 0),
            tile + new Vector2i(0, -1),
        };
        comp.CachedAdjacentTiles = adjacent;

        // cache the exchanger itself to the system
        var entry = new StructureHeatExchangerCacheEntry
        {
            EntityUid = uid,
            Tile = tile,
            Comp = comp,
            TempComp = tempComp,
        };

        if (!_cachedHeatExchangers.TryGetValue(parent, out var tiles))
        {
            _cachedHeatExchangers.Add(parent, new HashSet<StructureHeatExchangerCacheEntry>{entry});
            tiles = _cachedHeatExchangers[parent];
        }

        tiles.Add(entry);

        // cache nearby heat exchangers to this exchanger
        var validHeatExchangers = new List<StructureHeatExchangerCacheEntry>();
        foreach (var exchanger in tiles)
        {
            if (exchanger.EntityUid == uid)
                continue;

            if (!comp.CachedAdjacentTiles.Contains(exchanger.Tile))
                continue;

            exchanger.Comp.CachedAdjacentHeatExchangers ??= new();

            exchanger.Comp.CachedAdjacentHeatExchangers.Add(entry);

            validHeatExchangers.Add(exchanger);
        }

        comp.CachedAdjacentHeatExchangers = validHeatExchangers;
    }

    private void OnCompShutdown(EntityUid uid, StructureHeatExchangeComponent comp, ComponentShutdown ev)
    {
        if (!comp.Parent.HasValue)
            return;

        if (!_cachedHeatExchangers.TryGetValue(comp.Parent.Value, out var entries))
            return;

        var cached = entries.Where(e => e.EntityUid == uid).ToList();
        if (cached.Count == 0)
            return;
        var entry = cached.FirstOrDefault();


        // remove this exchanger from other exchangers' cache
        var adjacentHeatExchangers = new List<StructureHeatExchangerCacheEntry>();
        foreach (var exchanger in entries)
        {
            if (exchanger.EntityUid == uid)
                continue;

            comp.CachedAdjacentTiles ??= new();

            if (!comp.CachedAdjacentTiles.Contains(exchanger.Tile))
                return;

            exchanger.Comp.CachedAdjacentHeatExchangers ??= new();

            exchanger.Comp.CachedAdjacentHeatExchangers.Remove(entry);
        }

        // remove this exchanger from the system cache
        entries.RemoveWhere(e => e.EntityUid == uid);
    }

    private void ProcessDevice(EntityUid uid, StructureHeatExchangeComponent comp, AtmosDeviceUpdateEvent args)
    {
        if (!Enabled)
            return;

        if (!comp.Parent.HasValue)
            return;

        TemperatureComponent? temp = comp.TempComp;
        if (temp == null)
            return;
        float T_s = temp.CurrentTemperature;
        float k = temp.AtmosTemperatureTransferEfficiency;
        float c = temp.SpecificHeat;

        comp.CachedAdjacentHeatExchangers ??= new();

        // process heat transfer between solid heat exchangers
        foreach (var heatExchanger in comp.CachedAdjacentHeatExchangers)
        {
            // deltaT = T_2 - T_1
            float T_1 = heatExchanger.TempComp.CurrentTemperature;
            float deltaT = T_s - T_1;
            if (Math.Abs(deltaT) < 5) // TODO: cvar
                continue;
            // dQ = k * deltaT * dt
            float heatTransfer = k * deltaT * args.dt;

            // we shouldnt transfer more heat than is needed for equalization
            float structureHeatCap = heatExchanger.TempComp.SpecificHeat;
            float combinedHeat = c * temp.CurrentTemperature + structureHeatCap * heatExchanger.TempComp.CurrentTemperature;
            float combinedHeatCapacity = c + structureHeatCap;
            float equalizedTemp = combinedHeat / combinedHeatCapacity;
            float maxHeatTransfer = MathF.Abs(temp.CurrentTemperature - equalizedTemp) * c;

            heatTransfer *= Multiplier;

            if (heatTransfer > 0)
                heatTransfer = MathF.Min(heatTransfer, maxHeatTransfer);
            else
                heatTransfer = MathF.Max(heatTransfer, -maxHeatTransfer);

            heatExchanger.TempComp.CurrentTemperature += heatTransfer / structureHeatCap;
            temp.CurrentTemperature -= heatTransfer / c;
        }

        comp.CachedAdjacentTiles ??= new();

        // now process heat transfer with nearby atmosphere
        GasMixture?[]? mixtures = _atmosphereSystem.GetTileMixtures(comp.Parent.Value, null, comp.CachedAdjacentTiles);

        if (mixtures == null)
            return;

        // there are some gas mixtures that don't transfer heat or do it really slowly
        // we will ignore them
        // its a bit of a stretch, but we can assume that vacuum is a gas with P < 3 kPa
        List<GasMixture> validMixtures = new List<GasMixture>();
        foreach (var mixture in mixtures)
        {
            if (mixture == null)
                continue;

            if (mixture.Pressure > 3) // cvar
                validMixtures.Add(mixture);
        }

        if (validMixtures.Count == 0)
            return;

        // it behaves differently depending on the order of processing
        _random.Shuffle(validMixtures);

        foreach (var mixture in validMixtures)
        {
            // s - structure, g - gas
            // deltaT = T_s - T_g
            // coefficient of thermal conductivity in gases is proportional to sqrt(T_g/m_g)
            // ill take moles as a measure of mass, since i cant read it from outside of atmos systems. dont really care about the molar mass too
            // deltaQ = k * sqrt(T_g/m_g) * deltaT * dt
            float deltaT = temp.CurrentTemperature - mixture.Temperature;
            if (Math.Abs(deltaT) < 5) // TODO: cvar
                continue;

            float heatTransfer = temp.AtmosTemperatureTransferEfficiency
                               * MathF.Sqrt(mixture.Temperature / mixture.TotalMoles)
                               * deltaT
                               * args.dt;
                               //  / _atmosphereSystem.HeatScale; //  you can also apply scaling

            // we shouldnt transfer more heat than is needed for equalization
            float gasHeatCap = _atmosphereSystem.GetHeatCapacity(mixture, false);
            float combinedHeat = temp.SpecificHeat * temp.CurrentTemperature + gasHeatCap * mixture.Temperature;
            float combinedHeatCapacity = temp.SpecificHeat + gasHeatCap;
            float equalizedTemp = combinedHeat / combinedHeatCapacity;
            float maxHeatTransfer = MathF.Abs(temp.CurrentTemperature - equalizedTemp) * temp.SpecificHeat;

            heatTransfer *= Multiplier;

            if (heatTransfer > 0)
                heatTransfer = MathF.Min(heatTransfer, maxHeatTransfer);
            else
                heatTransfer = MathF.Max(heatTransfer, -maxHeatTransfer);

            _atmosphereSystem.AddHeat(mixture, heatTransfer);
            temp.CurrentTemperature -= heatTransfer / temp.SpecificHeat;
        }
    }

    // public void GlobalGoida()
    // {
    //     var query = EntityQueryEnumerator<TagComponent>();
    //     while (query.MoveNext(out var uid, out var tag))
    //     {
    //         if (!tag.Tags.Contains("Window") && !tag.Tags.Contains("Wall"))
    //             continue;
    //
    //         var atmosDevice = EnsureComp<AtmosDeviceComponent>(uid);
    //         atmosDevice.RequireAnchored = false;
    //
    //         var temp = EnsureComp<TemperatureComponent>(uid);
    //         temp.SpecificHeat = 5000;
    //         temp.AtmosTemperatureTransferEfficiency = 10;
    //         temp.ColdDamageThreshold = 10;
    //         temp.HeatDamageThreshold = 100000;
    //
    //         EnsureComp<StructureHeatExchangeComponent>(uid);
    //     }
    // }
}

public struct StructureHeatExchangerCacheEntry
{
    public EntityUid EntityUid;
    public Vector2i Tile;
    public StructureHeatExchangeComponent Comp;
    public TemperatureComponent TempComp;
}

