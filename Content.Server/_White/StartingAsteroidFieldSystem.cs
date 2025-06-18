using Content.Server.GameTicking;
using Content.Server.Gravity;
using Content.Server.Procedural;
using Content.Server.RequiresGrid;
using Content.Server.Salvage;
using Content.Server.Salvage.Magnet;
using Content.Server.Shuttles.Systems;
using Content.Shared._White;
using Content.Shared._White.CCVar;
using Content.Shared.Procedural;
using Content.Shared.Salvage.Magnet;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Server.Console.Commands;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Content.Server._White;

public sealed class StartingAsteroidFieldSystem : EntitySystem
{
    [Dependency] private readonly SalvageSystem _salvage = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly GravitySystem _gravity = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ShuttleSystem _shittle = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
       
    }

    const float spawningBoxSize = 1000;

    private void OnPostGameMapLoad(PostGameMapLoad ev)
    {
        if (!_config.GetCVar(WhiteCVars.AsteroidFieldEnabled))
        {
            Log.Info("Asteroid field disabled. Skipping generation.");
            return;
        }
        float min = _config.GetCVar(WhiteCVars.AsteroidFieldDistanceMin);
        float max = _config.GetCVar(WhiteCVars.AsteroidFieldDistanceMax);
        int asteroids = _config.GetCVar(WhiteCVars.AsteroidFieldAsteroidCount);
        int derelicts = _config.GetCVar(WhiteCVars.AsteroidFieldDerelictCount);

        Vector2 worldPos = _random.NextAngle().ToWorldVec() * _random.NextFloat(min, max);

        if (!_config.GetCVar(WhiteCVars.AsteroidFieldSpawnBeacon))
            Log.Info($"Asteroid beacon spawning disabled. Skipping.");
        else
        {
            string path = _config.GetCVar(WhiteCVars.AsteroidFieldBeaconGridPath);

            var mapParams = new MapLoadOptions();
            mapParams.TransformMatrix = Matrix3Helpers.CreateTranslation(worldPos);
            mapParams.TransformMatrix = Matrix3x2.Multiply(Matrix3Helpers.CreateRotation(_random.NextAngle()), mapParams.TransformMatrix);

            if (!_map.TryLoad(ev.Map, path, out var grids, mapParams))
                Log.Info($"Failed to load asteroid beacon ({path}).");
            else
            {
                string beaconName = Loc.GetString(_config.GetCVar(WhiteCVars.AsteroidFieldBeaconName));
                _metaData.SetEntityName(grids[0], beaconName);
                Log.Info($"Successfully loaded asteroid beacon and named it \"{beaconName}\".");
            }
        }

        Log.Info($"Starting asteroid field generation at position {worldPos}...");
        SpawnAsteroidField(ev.Map, worldPos, asteroids, derelicts);
    }

    // todo: rewrite grid spawn logic instead of copypasting salvage magnet code?
    private async Task SpawnAsteroidField(MapId mapId, Vector2 worldPos, int asteroidAmount, int derelictAmount)
    {
        List<MapId> maps = new();
        List<Task> dungeonTasks = new();
        int c = 1;
        int total = asteroidAmount + derelictAmount;

        Log.Debug($"Queuing generation of {asteroidAmount} asteroids.");
        for(int i = 0; i < asteroidAmount; i++)
        {
            var salvMap = _mapManager.CreateMap();
            var seed = _random.Next();
            seed = seed - seed % 2; // asteroid map
            var asteroid = (AsteroidOffering) _salvage.GetSalvageOffering(seed);
            var grid = _mapManager.CreateGrid(salvMap);
            Log.Debug($"Queuing asteroid generation. ({i+1}/{asteroidAmount})");
            var task = _dungeon.GenerateDungeonAsync(asteroid.DungeonConfig, grid.Owner, grid, Vector2i.Zero, seed);
            var finishtask = task.ContinueWith(_ => { Log.Info($"Finished generating asteroid {c}/{asteroidAmount}."); c++; });
            dungeonTasks.Add(finishtask);
            maps.Add(salvMap);
        }

        await Task.WhenAll(dungeonTasks);
        Log.Info($"Asteroids generated. Now generating derelicts for asteroid field.");

        for (int i = 0; i < derelictAmount; i++)
        {
            var salvMap = _mapManager.CreateMap();
            var seed = _random.Next();
            seed = seed - seed % 2 + 1; // derelict map
            var salvage = (SalvageOffering) _salvage.GetSalvageOffering(seed);
            var opts = new MapLoadOptions
            {
                Offset = new Vector2(0, 0)
            };
            Log.Debug($"Generating derelict... ({i+1}/{derelictAmount})");
            _map.TryLoad(salvMap, salvage.SalvageMap.MapPath.ToString(), out var roots, opts);
            maps.Add(salvMap);
        }
        Log.Info($"Derelicts generated.");
        Log.Debug($"Total asteroid field counts: {asteroidAmount} asteroids, {derelictAmount} derelicts, {total} total.");

        maps.OrderBy(_ => _random.Next());

        foreach (var map in maps)
        {
            Box2? bounds = null;
            var mapXform = Transform(_mapManager.GetMapEntityId(map));

            if (mapXform.ChildCount == 0)
            {
                Log.Error($"Map {map} used for asteroid field generation had zero children. Skipping.");
                continue;
            }

            var mapChildren = mapXform.ChildEnumerator;

            while (mapChildren.MoveNext(out var mapChild))
            {
                // If something went awry in dungen.
                if (!TryComp<MapGridComponent>(mapChild, out var childGrid))
                {
                    Log.Error($"Map child {ToPrettyString(mapChild)} of map {map} used for asteroid field generation had no MapGridComponent. Skipping.");
                    continue;
                }

                var childAABB = _transform.GetWorldMatrix(mapChild).TransformBox(childGrid.LocalAABB);
                bounds = bounds?.Union(childAABB) ?? childAABB;

                //// Update mass scanner names as relevant.
                //if (offering is AsteroidOffering)
                //{
                //    //_metaData.SetEntityName(mapChild, Loc.GetString("salvage-asteroid-name"));
                //    _gravity.EnableGravity(mapChild);
                //}
            }

            var attachedBounds = new Box2Rotated(Box2.CenteredAround(worldPos, new(spawningBoxSize)));
            Angle worldAngle = _random.NextAngle();

            if (!_salvage.TryGetSalvagePlacementLocation(mapId, attachedBounds, bounds!.Value, worldAngle, out var spawnLocation, out var spawnAngle, 200, 0.10f))
            {
                Log.Error("Failed to find place to put grid in the asteroid field.");
                _mapManager.DeleteMap(map);
                continue;
            }

            mapChildren = mapXform.ChildEnumerator;

            while (mapChildren.MoveNext(out var mapChild))
            {
                var childXform = Transform(mapChild);
                var localPos = childXform.LocalPosition;
                _transform.SetParent(mapChild, childXform, _mapManager.GetMapEntityId(spawnLocation.MapId));
                _transform.SetWorldPositionRotation(mapChild, spawnLocation.Position + localPos, spawnAngle, childXform);
                if (HasComp<MapGridComponent>(mapChild))
                {
                    _shittle.Disable(mapChild);
                    _shittle.SetIFFFlag(mapChild, IFFFlags.HideLabel);
                }

                // Handle mob restrictions
                var children = childXform.ChildEnumerator;

                while (children.MoveNext(out var child))
                {
                    if (!TryComp<SalvageMobRestrictionsComponent>(child, out var salvMob))
                        continue;

                    salvMob.LinkedEntity = mapChild;
                }
            }
            _mapManager.DeleteMap(map);
        }
    }
}
