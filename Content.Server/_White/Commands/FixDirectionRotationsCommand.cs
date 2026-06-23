using System.Linq;
using Content.Server.Administration;
using Content.Server.Power.Components;
using Content.Shared.Administration;
using Content.Shared.Tag;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.Server._White.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class FixDirectionRotationsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public string Command => "fixdirectionrotations";
    public string Description => "Sets rotation based on neighbor layout for entities with ForceDirectionFixRotations.";
    public string Help => $"Usage: {Command} <gridId> | {Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        EntityUid? gridId;

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        switch (args.Length)
        {
            case 0:
                if (shell.Player?.AttachedEntity is not { Valid: true } playerEntity)
                {
                    shell.WriteError("Only a player can run this command.");
                    return;
                }
                gridId = xformQuery.GetComponent(playerEntity).GridUid;
                break;
            case 1:
                if (!NetEntity.TryParse(args[0], out var idNet) || !_entManager.TryGetEntity(idNet, out var id))
                {
                    shell.WriteError($"{args[0]} is not a valid entity.");
                    return;
                }
                gridId = id;
                break;
            default:
                shell.WriteError(Help);
                return;
        }

        if (!gridId.HasValue || !_entManager.TryGetComponent<MapGridComponent>(gridId.Value, out var grid))
        {
            shell.WriteError("Failed to resolve grid.");
            return;
        }

        var tagSystem = _entManager.EntitySysManager.GetEntitySystem<TagSystem>();
        var changed = 0;

        var enumerator = xformQuery.GetComponent(gridId.Value).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (!tagSystem.HasTag(child, "ForceDirectionFixRotations"))
                continue;
            if (tagSystem.HasTag(child, "ForceNoDirectionFixRotations"))
                continue;

            var childXform = xformQuery.GetComponent(child);
            var tilePos = grid.TileIndicesFor(childXform.Coordinates);

            var neighborDirs = new List<Direction>();
            var offsets = new (Direction dir, Vector2i offset)[]
            {
                (Direction.North, new Vector2i(0, 1)),
                (Direction.South, new Vector2i(0, -1)),
                (Direction.East, new Vector2i(1, 0)),
                (Direction.West, new Vector2i(-1, 0))
            };

            foreach (var (dir, offset) in offsets)
            {
                var neighborTile = new Vector2i(tilePos.X + offset.X, tilePos.Y + offset.Y);
                var anchored = grid.GetAnchoredEntities(neighborTile);
                foreach (var ent in anchored)
                {
                    if (_entManager.HasComponent<CableComponent>(ent))
                        continue;
                    if (tagSystem.HasTag(ent, "Marker")) // For some reason it can't link to MarkerComponent, so a new tag was created.
                        continue;
                    if (tagSystem.HasTag(ent, "Table"))
                        continue;
                    if (tagSystem.HasTag(ent, "Carpet"))
                        continue;

                    bool validNeighbor = tagSystem.HasTag(ent, "ForceFixRotations");
                    if (!validNeighbor && _entManager.TryGetComponent<OccluderComponent>(ent, out var occluder) && occluder.Enabled)
                        validNeighbor = true;
                    if (validNeighbor)
                    {
                        neighborDirs.Add(dir);
                        break;
                    }
                }
            }

            Angle targetAngle;
            switch (neighborDirs.Count)
            {
                case 1:
                    var single = neighborDirs.First();
                    targetAngle = (single == Direction.East || single == Direction.West) ? Angle.Zero : Angle.FromDegrees(90);
                    break;
                case 2:
                    bool ns = neighborDirs.Contains(Direction.North) && neighborDirs.Contains(Direction.South);
                    bool ew = neighborDirs.Contains(Direction.East) && neighborDirs.Contains(Direction.West);
                    if (ns || ew)
                    {
                        targetAngle = ns ? Angle.FromDegrees(90) : Angle.Zero;
                    }
                    else
                    {
                        targetAngle = Angle.Zero;
                        var dirs = string.Join("-", neighborDirs.Select(d => d.ToString()));
                        shell.WriteLine($"Entity {child} at {tilePos} has 2 neighbors on a corner ({dirs}) → dealt to South");
                    }
                    break;
                case 3:
                    ns = neighborDirs.Contains(Direction.North) && neighborDirs.Contains(Direction.South);
                    ew = neighborDirs.Contains(Direction.East) && neighborDirs.Contains(Direction.West);
                    targetAngle = ew ? Angle.Zero : Angle.FromDegrees(90);
                    var pair = ew ? "East-West" : "North-South";
                    var single3 = neighborDirs.FirstOrDefault(d => !((ew && (d == Direction.East || d == Direction.West)) || (ns && (d == Direction.North || d == Direction.South))));
                    shell.WriteLine($"Entity {child} at {tilePos} has 3 neighbors: {pair} and {single3} → dealt to {(ew ? "South" : "West")}");
                    break;
                case 4:
                    targetAngle = Angle.Zero;
                    shell.WriteLine($"Entity {child} at {tilePos} has 4 neighbors → dealt to South");
                    break;
                default:
                    continue;
            }

            if (childXform.LocalRotation != targetAngle)
            {
                childXform.LocalRotation = targetAngle;
                changed++;
            }
        }

        shell.WriteLine($"Changed {changed} entities. If things seem wrong, reconnect.");
    }
}
