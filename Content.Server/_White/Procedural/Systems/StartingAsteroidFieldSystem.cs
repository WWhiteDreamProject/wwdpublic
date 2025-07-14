using Content.Server.GameTicking;
using Content.Server.Procedural;
using Content.Server.Salvage;
using Content.Server.Salvage.Magnet;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared._White.CCVar;
using Content.Shared.Salvage.Magnet;
using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server._White.Procedural.Systems;

public sealed class StartingAsteroidFieldSystem : EntitySystem
{
    [Dependency] private readonly SalvageSystem _salvage = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad, after: [typeof(StationSystem)]);
    }

    private const float SpawningBoxSize = 1000f;

    private void OnPostGameMapLoad(PostGameMapLoad ev)
    {
        if (!_config.GetCVar(WhiteCVars.AsteroidFieldEnabled))
        {
            Log.Info("Asteroid field disabled. Skipping generation.");
            return;
        }

        var min = _config.GetCVar(WhiteCVars.AsteroidFieldDistanceMin);
        var max = _config.GetCVar(WhiteCVars.AsteroidFieldDistanceMax);
        var asteroids = _config.GetCVar(WhiteCVars.AsteroidFieldAsteroidCount);
        var derelicts = _config.GetCVar(WhiteCVars.AsteroidFieldDerelictCount);

        var worldPos = _random.NextAngle().ToWorldVec() * _random.NextFloat(min, max);

        if (!_config.GetCVar(WhiteCVars.AsteroidFieldSpawnBeacon))
            Log.Info($"Asteroid beacon spawning disabled. Skipping.");
        else
        {
            var path = _config.GetCVar(WhiteCVars.AsteroidFieldBeaconGridPath);

            var mapParams = new MapLoadOptions();
            mapParams.TransformMatrix = Matrix3Helpers.CreateTranslation(worldPos);
            mapParams.TransformMatrix = Matrix3x2.Multiply(Matrix3Helpers.CreateRotation(_random.NextAngle()), mapParams.TransformMatrix);

            if (!_mapLoader.TryLoad(ev.Map, path, out var grids, mapParams))
                Log.Info($"Failed to load asteroid beacon ({path}).");
            else
            {
                var beaconName = Loc.GetString(_config.GetCVar(WhiteCVars.AsteroidFieldBeaconName));
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
        List<MapId> asteroidMaps = new();
        List<MapId> derelictMaps = new();
        List<MapId> maps = new();
        List<Task> loadTasks = new();

        for(int i = 0; i < asteroidAmount; i++)
            if (_map.CreateMap(out var asteroidMapId, false).Valid)
                asteroidMaps.Add(asteroidMapId);

        if (asteroidAmount != asteroidMaps.Count)
        {
            Log.Error($"Failed to create {asteroidAmount-asteroidMaps.Count} maps out of {asteroidAmount}. {asteroidMaps.Count} will instead be generated.");
            asteroidAmount = asteroidMaps.Count;
        }

        for(int i = 0; i < derelictAmount; i++)
            if (_map.CreateMap(out var derelictMapId, false).Valid)
                derelictMaps.Add(derelictMapId);

        if(derelictAmount != derelictMaps.Count)
        {
            Log.Error($"Failed to create {derelictAmount - derelictMaps.Count} maps out of {derelictAmount}. {derelictMaps.Count} will instead be generated.");
            derelictAmount = derelictMaps.Count;
        }

        var total = asteroidAmount + derelictAmount;
        int c = 1;

        for(int i = 0; i < asteroidAmount; i++)
        {
            var asteroidMap = asteroidMaps[i];
            var seed = _random.Next();
            seed -= seed % 2; // asteroid map

            var asteroid = (AsteroidOffering) _salvage.GetSalvageOffering(seed);
            var grid = _mapManager.CreateGridEntity(asteroidMap);

            Log.Debug($"Queuing asteroid generation. ({i+1}/{asteroidAmount})");

            var task = _dungeon.GenerateDungeonAsync(asteroid.DungeonConfig, grid, grid, Vector2i.Zero, seed);
            var finishTask = task.ContinueWith(_ =>
            {
                Log.Info($"Finished generating asteroid {c}/{asteroidAmount}.");
                c++;
            });

            loadTasks.Add(finishTask);
            maps.Add(asteroidMap);
        }


        for(int i = 0; i < derelictAmount; i++)
        {
            var derelictMap = derelictMaps[i];
            var seed = _random.Next();
            seed = seed - seed % 2 + 1; // derelict map

            var salvage = (SalvageOffering) _salvage.GetSalvageOffering(seed);
            var opts = new MapLoadOptions
            {
                Offset = new Vector2(0, 0)
            };

            Log.Debug($"Generating derelict... ({i+1}/{derelictAmount})");

            _mapLoader.TryLoad(derelictMap, salvage.SalvageMap.MapPath.ToString(), out _, opts);

            maps.Add(derelictMap);
        }

        await Task.WhenAll(loadTasks);
        Log.Debug($"Asteroid field maps generated. Total asteroid field counts: {asteroidAmount} asteroids, {derelictAmount} derelicts, {total} total.");

        _random.Shuffle(maps);
        foreach (var map in maps)
        {
            if (!_map.TryGetMap(map, out var mapUid) || !mapUid.HasValue)
                continue;

            Box2? bounds = null;
            var mapXform = Transform(mapUid.Value);

            if (mapXform.ChildCount == 0)
            {
                Log.Error($"Map {map} used for asteroid field generation had zero children. Skipping.");
                continue;
            }

            var mapChildren = mapXform.ChildEnumerator;

            while (mapChildren.MoveNext(out var mapChild))
            {
                // If something went awry in dungeon.
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

            var attachedBounds = new Box2Rotated(Box2.CenteredAround(worldPos, new(SpawningBoxSize)));
            var worldAngle = _random.NextAngle();

            if (!_salvage.TryGetSalvagePlacementLocation(mapId, attachedBounds, bounds!.Value, worldAngle, out var spawnLocation, out var spawnAngle, 200, 0.10f))
            {
                Log.Error($"Failed to find free space the asteroid field. Consider tweaking asteroid field size.");
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
                    _shuttle.Disable(mapChild);
                    _shuttle.SetIFFFlag(mapChild, IFFFlags.HideLabel);
                }

                // Handle mob restrictions
                var children = childXform.ChildEnumerator;

                while (children.MoveNext(out var child))
                {
                    if (!TryComp<SalvageMobRestrictionsComponent>(child, out var salvageMob))
                        continue;

                    salvageMob.LinkedEntity = mapChild;
                }
            }

            _mapManager.DeleteMap(map);
        }
        Log.Info($"Finished generating asteroid field at {worldPos}.");
    }
}
