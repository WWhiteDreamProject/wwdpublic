using Content.Server.GameTicking;
using Content.Server.Gravity;
using Content.Server.Procedural;
using Content.Server.Salvage;
using Content.Server.Salvage.Magnet;
using Content.Shared.Salvage.Magnet;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
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
    [Dependency] private readonly SharedShuttleSystem _shittle = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
    }

    const float spawningBoxSize = 500;

    private void OnPostGameMapLoad(PostGameMapLoad ev)
    {

        const int amount = 50;
        const double ruinPercentage = 0.13;
        bool[] parameters = new bool[amount];
        Array.Fill(parameters, true, 0, (int) Math.Round(amount * ruinPercentage));
        parameters = parameters.OrderBy(_ => _random.Next()).ToArray();
        for (int i = 0; i < amount; i++)
            SpawnAsteroid(ev.Map, new Vector2(2000, 2000), parameters[i]).Wait();

    }

    private async Task SpawnAsteroid(MapId mapId, Vector2 worldPos, bool ruin)
    {
        var seed = _random.Next();
        seed -= seed % 2; // asteroid map
        if (ruin)
            seed++; // space ruin map

        var offering = _salvage.GetSalvageOffering(seed);

        var salvMap = _mapManager.CreateMap();

        switch (offering)
        {
            case AsteroidOffering asteroid:
                var grid = _mapManager.CreateGrid(salvMap);
                await _dungeon.GenerateDungeonAsync(asteroid.DungeonConfig, grid.Owner, grid, Vector2i.Zero, seed);
                break;
            case SalvageOffering wreck:
                var salvageProto = wreck.SalvageMap;

                var opts = new MapLoadOptions
                {
                    Offset = new Vector2(0, 0)
                };

                if (!_map.TryLoad(salvMap, salvageProto.MapPath.ToString(), out var roots, opts))
                {
                    _mapManager.DeleteMap(salvMap);
                    return;
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        Box2? bounds = null;
        var mapXform = Transform(_mapManager.GetMapEntityId(salvMap));

        if (mapXform.ChildCount == 0)
        {
            return;
        }

        var mapChildren = mapXform.ChildEnumerator;

        while (mapChildren.MoveNext(out var mapChild))
        {
            // If something went awry in dungen.
            if (!TryComp<MapGridComponent>(mapChild, out var childGrid))
                continue;

            var childAABB = _transform.GetWorldMatrix(mapChild).TransformBox(childGrid.LocalAABB);
            bounds = bounds?.Union(childAABB) ?? childAABB;

            // Update mass scanner names as relevant.
            if (offering is AsteroidOffering)
            {
                //_metaData.SetEntityName(mapChild, Loc.GetString("salvage-asteroid-name"));
                _gravity.EnableGravity(mapChild);
            }
        }

        var attachedBounds = new Box2Rotated(Box2.CenteredAround(worldPos, new(spawningBoxSize)));
        Angle worldAngle = _random.NextAngle();

        if (!_salvage.TryGetSalvagePlacementLocation(mapId, attachedBounds, bounds!.Value, worldAngle, out var spawnLocation, out var spawnAngle, 200, 0.10f))
        {
            Log.Error("Failed to find place to put grid in the asteroid field.");
            _mapManager.DeleteMap(salvMap);
            return;
        }

        mapChildren = mapXform.ChildEnumerator;

        // It worked, move it into position and cleanup values.
        while (mapChildren.MoveNext(out var mapChild))
        {
            var salvXForm = Transform(mapChild);
            var localPos = salvXForm.LocalPosition;
            _transform.SetParent(mapChild, salvXForm, _mapManager.GetMapEntityId(spawnLocation.MapId));
            _transform.SetWorldPositionRotation(mapChild, spawnLocation.Position + localPos, spawnAngle, salvXForm);
            var iff = EnsureComp<IFFComponent>(mapChild);
#pragma warning disable RA0002 // go fuck yourself
            iff.Flags = IFFFlags.HideLabel;
#pragma warning restore RA0002

            // Handle mob restrictions
            var children = salvXForm.ChildEnumerator;

            while (children.MoveNext(out var child))
            {
                if (!TryComp<SalvageMobRestrictionsComponent>(child, out var salvMob))
                    continue;

                salvMob.LinkedEntity = mapChild;
            }
        }
        _mapManager.DeleteMap(salvMap);

    }
}
