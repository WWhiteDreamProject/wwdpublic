using Content.Server._White.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Content.Shared._White.SpawnOnGamerule.Components;
using Content.Shared._White.SpawnOnGamerule.Events;
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
        RemoveTiles(component.TilesToRemove);
        RemoveEntitiesByTags(component.EntityTagsToRemove);
        SpawnOnGamerule(component.MarkersToActivate);
    }

    private void RemoveTiles(List<string> tilePrototypes)
    {
        var tileDefManager = IoCManager.Resolve<ITileDefinitionManager>();
        var query = EntityQueryEnumerator<MapGridComponent>();

        while (query.MoveNext(out var gridUid, out var grid))
        {
            foreach (var tileRef in grid.GetAllTiles())
            {
                var tileDef = (ContentTileDefinition)tileDefManager[tileRef.Tile.TypeId];
                if (tilePrototypes.Contains(tileDef.ID))
                {
                    _mapSystem.SetTile(gridUid, grid, tileRef.GridIndices, Tile.Empty);
                }
            }
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
