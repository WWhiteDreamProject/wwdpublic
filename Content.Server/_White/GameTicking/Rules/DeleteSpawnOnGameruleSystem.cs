using Content.Server._White.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Content.Server._White.Spawners.Components;
using Content.Server._White.Spawners.Events;
using Content.Shared.Tag;
using Content.Shared.GameTicking.Components;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.IoC;

namespace Content.Server._White.GameTicking.Rules;

public sealed class DeleteSpawnOnGameruleSystem : GameRuleSystem<DeleteSpawnOnGameruleComponent>
{
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    protected override void Started(EntityUid uid, DeleteSpawnOnGameruleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        SpawnOnGamerule(component.MarkersToActivate);
        RemoveEntitiesByTags(component.EntityTagsToRemove);
        RemoveTiles(component.TilesToRemove);
    }

    private void RemoveTiles(List<string> tilePrototypes)
    {
        var tileDefManager = IoCManager.Resolve<ITileDefinitionManager>();
        var maxIterations = 5;
        var iteration = 0;

        while (iteration < maxIterations)
        {
            var tilesToRemove = new List<(EntityUid gridUid, MapGridComponent grid, Vector2i indices)>();
            var gridQuery = EntityQueryEnumerator<MapGridComponent>();

            while (gridQuery.MoveNext(out var gridUid, out var grid))
            {
                foreach (var tileRef in grid.GetAllTiles())
                {
                    var tileDef = (ContentTileDefinition)tileDefManager[tileRef.Tile.TypeId];
                    if (tilePrototypes.Contains(tileDef.ID))
                    {
                        tilesToRemove.Add((gridUid, grid, tileRef.GridIndices));
                    }
                }
            }

            if (tilesToRemove.Count == 0)
                break;

            foreach (var (gridUid, grid, indices) in tilesToRemove)
            {
                _mapSystem.SetTile(gridUid, grid, indices, Tile.Empty);
            }

            iteration++;
        }
    }

    private void RemoveEntitiesByTags(List<string> tags)
    {
        var query = EntityQueryEnumerator<TagComponent>();
        while (query.MoveNext(out var uid, out var tagComp))
        {
            foreach (var tag in tags)
            {
                if (_tagSystem.HasTag(uid, tag))
                {
                    EntityManager.QueueDeleteEntity(uid);
                    break;
                }
            }
        }
    }

    private void SpawnOnGamerule(List<string> markerPrototypes)
    {
        var query = EntityQueryEnumerator<SpawnOnGameruleComponent>();
        while (query.MoveNext(out var uid, out var marker))
        {
            var proto = Prototype(uid);
            if (proto != null && markerPrototypes.Contains(proto.ID))
            {
                RaiseLocalEvent(uid, new ActivateMarkerEvent());
            }
        }
    }
}
