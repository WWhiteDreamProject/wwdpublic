using Content.Shared._White.Actions;
using Content.Shared.Coordinates;
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

    public override void Initialize()
    {
        SubscribeLocalEvent<SpawnTileEntityActionEvent>(OnSpawnTileEntityAction);
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
}
