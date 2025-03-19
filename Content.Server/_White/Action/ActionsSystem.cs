using Content.Server.DoAfter;
using Content.Shared._White.Actions;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._White.Action;

public sealed class ActionsSystem : EntitySystem
{
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpawnTileEntityActionEvent>(OnSpawnTileEntityAction);
        SubscribeLocalEvent<PlaceTileEntityEvent>(OnPlaceTileEntityEvent);

        SubscribeLocalEvent<PlaceTileEntityDoAfterEvent>(OnPlaceTileEntityDoAfter);
    }

    private void OnSpawnTileEntityAction(SpawnTileEntityActionEvent args)
    {
        if (args.Handled)
            return;

        var coordinates = args.Performer.ToCoordinates();

        if (args.TileId is { } tileId)
        {
            if (_transform.GetGrid(coordinates) is not { } grid || !TryComp(grid, out MapGridComponent? mapGrid))
                return;

            var tileDef = _tileDef[tileId];
            var tile = new Tile(tileDef.TileId);

            _mapSystem.SetTile(grid, mapGrid, coordinates, tile);
        }

        if (args.Entity is { } entProtoId)
            Spawn(entProtoId, coordinates);

        if (args.Audio is { } audio)
            _audio.PlayPvs(audio, coordinates);

        args.Handled = true;
    }

    private void OnPlaceTileEntityEvent(PlaceTileEntityEvent args)
    {
        if (args.Handled)
            return;

        if (args.Length != 0)
        {
            var ev = new PlaceTileEntityDoAfterEvent
            {
                Target = GetNetCoordinates(args.Target),
                Entity = args.Entity,
                TileId = args.TileId,
                Audio = args.Audio
            };

            var doAfter = new DoAfterArgs(EntityManager, args.Performer, args.Length, ev, null)
            {
                BlockDuplicate = true,
                BreakOnDamage = true,
                CancelDuplicate = true,
                BreakOnMove = true,
                Broadcast = true
            };

            _doAfter.TryStartDoAfter(doAfter);
            return;
        }

        if (args.TileId is { } tileId)
        {
            if (_transform.GetGrid(args.Target) is not { } grid || !TryComp(grid, out MapGridComponent? mapGrid))
                return;

            var tileDef = _tileDef[tileId];
            var tile = new Tile(tileDef.TileId);

            _mapSystem.SetTile(grid, mapGrid, args.Target, tile);
        }

        if (args.Entity is { } entProtoId)
            Spawn(entProtoId, args.Target);

        if (args.Audio is { } audio)
            _audio.PlayPvs(audio, args.Target);

        args.Handled = true;
    }

    private void OnPlaceTileEntityDoAfter(PlaceTileEntityDoAfterEvent args)
    {
        if (args.Handled)
            return;

        var coordinates = GetCoordinates(args.Target);

        if (args.TileId is { } tileId)
        {
            if (_transform.GetGrid(coordinates) is not { } grid || !TryComp(grid, out MapGridComponent? mapGrid))
                return;

            var tileDef = _tileDef[tileId];
            var tile = new Tile(tileDef.TileId);

            _mapSystem.SetTile(grid, mapGrid, coordinates, tile);
        }

        if (args.Entity is { } entProtoId)
            Spawn(entProtoId, coordinates);

        if (args.Audio is { } audio)
            _audio.PlayPvs(audio, coordinates);

        args.Handled = true;
    }
}
