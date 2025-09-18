using Content.Server.Database;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Shared._White.CustomGhostSystem;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._White.Commands;



[AnyCommand]
public sealed class ListCustomGhostsCommand : IConsoleCommand
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IServerPreferencesManager _prefMan = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public string Command => "listcustomghosts";
    public string Description => Loc.GetString("listcustomghosts-command-description");
    public string Help => Loc.GetString("listcustomghosts-command-help-text");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {

        var protos = _proto.EnumeratePrototypes<CustomGhostPrototype>();
        var sb = new StringBuilder();
        if (shell.Player is not ICommonSession player)
        {
            foreach (var proto in protos)
                sb.AppendLine(proto.ID);

            shell.WriteLine(sb.ToString());
            return;
        }

        if (args.Length > 0)
        {
            shell.WriteLine(Help);
            return;
        }

        sb.AppendLine(Loc.GetString("listcustomghosts-available-ghosts"));
        var playtimes = await _db.GetPlayTimes(player.UserId);
        foreach(var proto in protos)
        {
            if (proto.Ckey is string ckey && ckey != player.Name)
                continue;

            sb.Append(proto.ID);

            if (proto.PlaytimeHours is not null)
            {
                var trackers = proto.PlaytimeHours.Keys;

                foreach (var tracker in trackers)
                {
                    float hoursPlayed = (float) _playTimeTracking.GetPlayTimeForTracker(player, tracker).TotalHours;
                    float hoursRequired = proto.PlaytimeHours[tracker];

                    if (hoursPlayed < hoursRequired)
                    {
                        sb.Append(" [x]");
                        break;
                    }
                }
            }
            sb.Append("\n");

        }

        shell.WriteLine(sb.ToString());
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args) =>
        args.Length switch
        {
            1 => CompletionResult.FromHint("all"),
            _ => CompletionResult.Empty
        };
}
