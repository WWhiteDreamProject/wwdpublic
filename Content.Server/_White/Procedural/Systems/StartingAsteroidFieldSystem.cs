using System.Linq;
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
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Numerics;
using System.Threading.Tasks;
using Robust.Shared.EntitySerialization.Systems;
using Content.Shared.Salvage;

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
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad, after: [typeof(StationSystem),]);
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

            if (!_mapLoader.TryLoadGrid(ev.Map, path, out var grid, offset:worldPos, rot:_random.NextAngle()))
                Log.Info($"Failed to load asteroid beacon ({path}).");
            else
            {
                var beaconName = Loc.GetString(_config.GetCVar(WhiteCVars.AsteroidFieldBeaconName));
                _metaData.SetEntityName(grid.Value, beaconName);
                Log.Info($"Successfully loaded asteroid beacon and named it \"{beaconName}\".");
            }
        }

        Log.Info($"Starting asteroid field generation at position {worldPos}...");
        _ = SpawnAsteroidField(ev.Map, worldPos, asteroids, derelicts);
    }

    // todo: rewrite grid spawn logic instead of copypasting salvage magnet code?
    private async Task SpawnAsteroidField(MapId mapId, Vector2 worldPos, int asteroidAmount, int derelictAmount)
    {
        List<MapId> asteroidMaps = new();
        List<MapId> derelictMaps = new();
        List<MapId> maps = new();
        List<Task> loadTasks = new();

        for (int i = 0; i < asteroidAmount; i++)
            if (_map.CreateMap(out var asteroidMapId, false).Valid)
                asteroidMaps.Add(asteroidMapId);

        if (asteroidAmount != asteroidMaps.Count)
        {
            Log.Error($"Failed to create {asteroidAmount-asteroidMaps.Count} maps out of {asteroidAmount}. {asteroidMaps.Count} will instead be generated.");
            asteroidAmount = asteroidMaps.Count;
        }

        for (int i = 0; i < derelictAmount; i++)
            if (_map.CreateMap(out var derelictMapId, false).Valid)
                derelictMaps.Add(derelictMapId);

        if (derelictAmount != derelictMaps.Count)
        {
            Log.Error($"Failed to create {derelictAmount - derelictMaps.Count} maps out of {derelictAmount}. {derelictMaps.Count} will instead be generated.");
            derelictAmount = derelictMaps.Count;
        }

        var total = asteroidAmount + derelictAmount;
        int c = 1;

        for (int i = 0; i < asteroidAmount; i++)
        {
            var asteroidMap = asteroidMaps[i];
            var seed = _random.Next();

            var asteroid = (AsteroidOffering) _salvage.GetSalvageOffering(seed, SharedSalvageSystem.SalvageMagnetOfferingTypeEnum.Asteroid);
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


        for (int i = 0; i < derelictAmount; i++)
        {
            var derelictMap = derelictMaps[i];
            var seed = _random.Next();

            var salvage = (SalvageOffering) _salvage.GetSalvageOffering(seed, SharedSalvageSystem.SalvageMagnetOfferingTypeEnum.Salvage);

            Log.Debug($"Generating derelict... ({i+1}/{derelictAmount})");

            _mapLoader.TryLoadGrid(derelictMap, salvage.SalvageMap.MapPath, out _, offset: new (0, 0));

            maps.Add(derelictMap);
        }

        await Task.WhenAll(loadTasks);
        Log.Debug($"Asteroid field maps generated. Total asteroid field counts: {asteroidAmount} asteroids, {derelictAmount} derelicts, {total} total.");

        _random.Shuffle(maps);
        foreach (var map in maps)
        {
            if (!_map.TryGetMap(map, out var mapUid))
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

            if (!TryGetPlacementLocation(mapId, attachedBounds, bounds!.Value, worldAngle, out var spawnLocation, out var spawnAngle))
            {
                Log.Error($"Failed to find free space the asteroid field. Consider tweaking asteroid field size.");
                _map.DeleteMap(map);
                continue;
            }

            mapChildren = mapXform.ChildEnumerator;

            while (mapChildren.MoveNext(out var mapChild))
            {
                var childXform = Transform(mapChild);
                var localPos = childXform.LocalPosition;

                _transform.SetParent(mapChild, childXform, _map.GetMap(spawnLocation.MapId));
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

            _map.DeleteMap(map);
        }
        Log.Info($"Finished generating asteroid field at {worldPos}.");
    }

    private bool TryGetPlacementLocation(MapId mapId, Box2Rotated attachedBounds, Box2 bounds, Angle worldAngle, out MapCoordinates coords, out Angle angle, int iter = 200, float step = 0.10f)
    {
        var attachedAABB = attachedBounds.CalcBoundingBox();
        var minDistance = (attachedAABB.Height < attachedAABB.Width ? attachedAABB.Width : attachedAABB.Height) / 2f;
        var minActualDistance = bounds.Height < bounds.Width ? minDistance + bounds.Width / 2f : minDistance + bounds.Height / 2f;

        var attachedCenter = attachedAABB.Center;
        var fraction = step;

        for (var i = 0; i < iter; i++)
        {
            var randomPos = attachedCenter +
                worldAngle.ToVec() * (minActualDistance * fraction);
            var finalCoords = new MapCoordinates(randomPos, mapId);

            angle = _random.NextAngle();
            var box2 = Box2.CenteredAround(finalCoords.Position, bounds.Size);
            var box2Rot = new Box2Rotated(box2, angle, finalCoords.Position);

            // This doesn't stop it from spawning on top of random things in space
            // Might be better like this, ghosts could stop it before
            if (_mapManager.FindGridsIntersecting(finalCoords.MapId, box2Rot).Any())
            {
                // Bump it further and further just in case.
                fraction += step;
                continue;
            }

            coords = finalCoords;
            return true;
        }

        angle = Angle.Zero;
        coords = MapCoordinates.Nullspace;
        return false;
    }
}
